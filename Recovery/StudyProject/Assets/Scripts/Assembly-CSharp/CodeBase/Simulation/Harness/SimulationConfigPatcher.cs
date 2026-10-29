using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Balance;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Location;
using Codebase.Player.Inventory;
using Codebase.Simulation.Rebuild;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using CodeBase.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Codebase.Simulation.Harness
{
	public sealed partial class SimulationHarness
	{
		// C9: applies job.patch before IsRunning (and before C4's override). A patch error fails the job with exit 5,
		// unless patch_on_error = record (empty jobs), which only records it in applied_patch.json.
		partial void RebuildJobApplyPatch(SimulationRunRequest.Job current, JObject jobManifest)
		{
			if (current.Patch == null) return;
			var patcher = new SimulationConfigPatcher(request, current, SimulationSandbox.JobIndex);
			jobManifest["patch"] = patcher.Apply();
			SimulationConfigPatcher.PatchError error = patcher.Error;
			if (error == null || current.PatchOnError == SimulationRunRequest.PatchOnErrorRecord) return;
			Fail(ExitBadRequest, "patch: " + error);
			// Leaves StartJob before IsRunning; Update logs it, the exit code stays 5.
			throw new InvalidOperationException("job " + current.JobId + " stopped: patch " + error.Code);
		}
	}

	// In-memory config patcher (design 6.1-6.2; unit C9). Applies Python's expanded patch to the live config objects
	// with the semantics of tests/patch_cases/README.md (tufu_balance.model.patch is the other side), writes the
	// net leaves to applied_patch.json and puts every value back when Play ends (Application.quitting), comparing
	// a digest of each patched object before the patch and after the restore.
	//
	// Nodes: <Root>.<path>[.<list>[].<path>]; roots CombatBalanceConfig, AmuletConfig, SimulationConfig,
	// LocationsConfig, ABTestDataConfig (the live single assets), ItemConfig, ArmorConfig (every asset of the type;
	// row {"asset"}), EnemySpawnerConfig (every scene spawner component; row {"scene", "file_id"} through C1's
	// identity map; Stats[<StatType>] is that stat's BaseValue). Fields are resolved by reflection, so a dictionary
	// node Python passes through (not in its model) works when its C# field exists; anything else is unknown_node.
	internal sealed class SimulationConfigPatcher
	{
		public const string Schema = "tufu.sim.applied_patch/1";
		public const string FileName = "applied_patch.json";
		private const string LogTag = "[TufuSim] ";
		private const BindingFlags DeclaredFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		private static readonly string[] OverrideKeys = { "node", "set", "add", "mul", "index", "where", "note" };
		private static readonly string[] Ops = { "set", "add", "mul" };
		private static readonly string[] PointLists = { "IslandPoints", "TimingPoints", "CoefPoints" };

		private static readonly Dictionary<string, (Type Type, Table Table)> Roots = new Dictionary<string, (Type, Table)>
		{
			{ "CombatBalanceConfig", (typeof(CombatBalanceConfig), Table.Single) },
			{ "AmuletConfig", (typeof(AmuletConfig), Table.Single) },
			{ "SimulationConfig", (typeof(SimulationConfig), Table.Single) },
			{ "LocationsConfig", (typeof(LocationsConfig), Table.Single) },
			{ "ABTestDataConfig", (typeof(ABTestDataConfig), Table.Single) },
			{ "ItemConfig", (typeof(ItemConfig), Table.Assets) },
			{ "ArmorConfig", (typeof(ArmorConfig), Table.Assets) },
			{ "EnemySpawnerConfig", (typeof(EnemySpawnerConfig), Table.Spawners) },
		};

		private static SimulationConfigPatcher pendingRestore;

		private readonly SimulationRunRequest request;
		private readonly SimulationRunRequest.Job job;
		private readonly int jobIndex;
		private readonly JObject document = new JObject();
		private readonly List<Action> undo = new List<Action>();
		private readonly Dictionary<string, Touched> touched = new Dictionary<string, Touched>();
		private readonly List<Touched> touchOrder = new List<Touched>();
		private readonly Dictionary<string, OwnerState> owners = new Dictionary<string, OwnerState>();
		private readonly Dictionary<string, List<Row>> rowCache = new Dictionary<string, List<Row>>();
		private readonly Dictionary<Type, List<(string Path, Object Asset)>> assetCache = new Dictionary<Type, List<(string, Object)>>();
		private readonly SortedSet<string> abTests = new SortedSet<string>(StringComparer.Ordinal);
		private Dictionary<string, (string Scene, long FileId)> spawnerIds;
		private readonly SortedSet<string> unmappedSpawners = new SortedSet<string>(StringComparer.Ordinal);
		private bool restored;

		public PatchError Error { get; private set; }

		public SimulationConfigPatcher(SimulationRunRequest request, SimulationRunRequest.Job job, int jobIndex)
		{
			this.request = request;
			this.job = job;
			this.jobIndex = jobIndex;
		}

		// ---- types ----

		private enum Table { Single, List, Assets, AssetList, Spawners }

		private enum Kind { Float, Double, Int, Bool, String, Ref, Enum, Other }

		public sealed class PatchError : Exception
		{
			public readonly string Code;
			public readonly int? Override;
			public readonly string Node;
			public readonly List<string> Nodes;

			public PatchError(string code, string message, int? index = null, string node = null, List<string> nodes = null) : base(message)
			{
				Code = code;
				Override = index;
				Node = node;
				Nodes = nodes;
			}

			public override string ToString() =>
				"[" + Code + "] " + (Override.HasValue ? "override " + Override.Value + (Node != null ? " (" + Node + ")" : "") + ": " : "") + Message;

			public JObject ToJson() => new JObject
			{
				["code"] = Code,
				["message"] = Message,
				["override"] = Override.HasValue ? (JToken)Override.Value : JValue.CreateNull(),
				["node"] = Node,
				["nodes"] = Nodes != null ? new JArray(Nodes) : (JToken)JValue.CreateNull(),
			};
		}

		private sealed class Override
		{
			public int Index;
			public string Node, Op;
			public JToken Arg, IndexToken;
			public JObject Where;
		}

		private sealed class NodeSpec
		{
			public string Node, Root;
			public Table Table;
			public Type OwnerType, RowType, ValueType;
			public string[] ListPath = new string[0], LeafPath;
			public StatType? Stat;
			public Kind Kind;
			public bool IsArray, IsPointList;
		}

		private sealed class Row
		{
			public JObject Id;
			public string IdKey, OwnerKey;
			public object Owner;
			public IList List;
			public int Index = -1;
			public object Item;
			public EnemySpawner Spawner;
			public string AssetPath, AssetName;

			public object Get() => List != null ? List[Index] : Item;

			// Struct list elements are boxed copies: written back after a change.
			public void Put(object value)
			{
				if (List != null && value != null && value.GetType().IsValueType) List[Index] = value;
			}
		}

		private sealed class Touched
		{
			public NodeSpec Spec;
			public Row Row;
			public object Old;
		}

		private sealed class OwnerState
		{
			public object Owner;
			public string Before, Patched, After;
		}

		// ---- entry ----

		public JObject Apply()
		{
			document["schema"] = Schema;
			document["job_id"] = job.JobId;
			JArray ops = new JArray();
			int count = 0;
			try
			{
				JArray patch = ReadExactPatch();
				count = patch.Count;
				List<Override> overrides = Parse(patch);
				var specs = new Dictionary<string, NodeSpec>(StringComparer.Ordinal);
				var unknown = new List<string>();
				foreach (Override ov in overrides)
				{
					if (specs.ContainsKey(ov.Node) || unknown.Contains(ov.Node)) continue;
					NodeSpec spec = Resolve(ov.Node);
					if (spec == null) unknown.Add(ov.Node);
					else specs[ov.Node] = spec;
				}
				if (unknown.Count > 0)
					throw new PatchError("unknown_node", "the C# patcher does not support node(s): " + string.Join(", ", unknown), overrides.First(o => unknown.Contains(o.Node)).Index, unknown[0], unknown);
				foreach (Override ov in overrides) ops.Add(ApplyOverride(ov, specs[ov.Node]));
				if (abTests.Count > 0) document["ab_roundtrip"] = AbRoundTrip();
				document["leaves"] = NetLeaves(out int verified);
				document["verified_leaves"] = verified;
				foreach (OwnerState o in owners.Values) o.Patched = Digest(o.Owner);
				document["error"] = null;
			}
			catch (PatchError e)
			{
				Error = e;
			}
			catch (Exception e)
			{
				Error = new PatchError("bad_patch", e.GetType().Name + ": " + e.Message);
			}
			document["overrides"] = count;
			document["ops"] = ops;
			document["spawners"] = new JObject
			{
				["map"] = job.SpawnerMap?.Path,
				["map_sha256"] = job.SpawnerMap?.Sha256,
				["mapped"] = spawnerIds?.Count ?? 0,
				["unmapped_selected"] = new JArray(unmappedSpawners),
			};
			if (Error != null)
			{
				document["error"] = Error.ToJson();
				document["leaves"] = new JArray();
				Restore("error");
				Debug.LogWarning(LogTag + "patch error " + Error);
			}
			else
			{
				document["restore"] = null;
				Write();
				pendingRestore = this;
				Application.quitting -= RestorePending;
				Application.quitting += RestorePending;
#if UNITY_EDITOR
				UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
				UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
				Debug.Log(LogTag + "patch applied: " + count + " override(s), " + ((JArray)document["leaves"]).Count + " leaf(s)");
			}
			return new JObject
			{
				["file"] = FileName,
				["overrides"] = count,
				["leaves"] = ((JArray)document["leaves"]).Count,
				["error"] = Error?.Code,
				["ab_roundtrip"] = abTests.Count > 0 && Error == null,
			};
		}

		private static void RestorePending()
		{
			SimulationConfigPatcher p = pendingRestore;
			pendingRestore = null;
			Application.quitting -= RestorePending;
			p?.Restore("play_mode_exit");
		}

#if UNITY_EDITOR
		private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
		{
			if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
			UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
			RestorePending();
		}
#endif

		private void Restore(string when)
		{
			if (restored) return;
			restored = true;
			var errors = new JArray();
			for (int i = undo.Count - 1; i >= 0; i--)
			{
				try
				{
					undo[i]();
				}
				catch (Exception e)
				{
					errors.Add(e.GetType().Name + ": " + e.Message);
				}
			}
			var list = new JArray();
			bool ok = errors.Count == 0;
			foreach (var entry in owners.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				entry.Value.After = Digest(entry.Value.Owner);
				bool same = entry.Value.After == entry.Value.Before;
				ok &= same;
				list.Add(new JObject
				{
					["owner"] = entry.Key,
					["before"] = entry.Value.Before,
					["patched"] = entry.Value.Patched,
					["after"] = entry.Value.After,
					["equal"] = same,
				});
			}
			document["restore"] = new JObject { ["when"] = when, ["ok"] = ok, ["owners"] = list, ["errors"] = errors };
			Write();
			if (!ok) Debug.LogError(LogTag + "patch restore incomplete: " + document["restore"].ToString(Formatting.None));
		}

		private void Write()
		{
			try
			{
				string path = Path.Combine(request.JobDir(job), FileName);
				SimulationJson.WriteText(path, document.ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n");
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		// The job's patch read again from the request text: Newtonsoft's number parsing in the editor's Mono is
		// off by 1 ulp on some literals (Runbook 17), SimulationCanonicalJson reads them as Python wrote them.
		private JArray ReadExactPatch()
		{
			string marker = Path.Combine(SimulationSandbox.TempDir, SimulationSandbox.ActiveRequestFile);
			var active = SimulationJson.ReadFile<SimulationSandbox.ActiveRequest>(marker);
			JObject root = SimulationCanonicalJson.Parse(File.ReadAllText(active.RequestPath, Encoding.UTF8));
			if (!(root["jobs"] is JArray jobs) || jobIndex < 0 || jobIndex >= jobs.Count || !(jobs[jobIndex] is JObject j) ||
			    (string)j["job_id"] != job.JobId)
				throw new PatchError("bad_patch", "job " + job.JobId + " not found in " + active.RequestPath);
			if (!(j["patch"] is JArray patch)) throw new PatchError("bad_patch", "patch must be a list");
			return patch;
		}

		// ---- parsing (model.patch._parse_override) ----

		private static List<Override> Parse(JArray patch)
		{
			var result = new List<Override>();
			for (int i = 0; i < patch.Count; i++)
			{
				if (!(patch[i] is JObject raw)) throw new PatchError("bad_patch", "override must be a mapping, got " + patch[i].ToString(Formatting.None), i);
				var unknown = raw.Properties().Select(p => p.Name).Where(n => !OverrideKeys.Contains(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();
				string node = raw["node"] != null && raw["node"].Type == JTokenType.String ? (string)raw["node"] : null;
				if (unknown.Count > 0) throw new PatchError("bad_patch", "unknown keys " + string.Join(", ", unknown), i, node);
				if (string.IsNullOrEmpty(node)) throw new PatchError("bad_patch", "missing node", i);
				string[] ops = Ops.Where(op => raw.Property(op) != null).ToArray();
				if (ops.Length != 1) throw new PatchError("bad_op", "exactly one of set, add, mul required, got " + string.Join(", ", ops), i, node);
				JToken where = raw["where"];
				if (where != null && where.Type != JTokenType.Null && where.Type != JTokenType.Object)
					throw new PatchError("bad_where", "where must be a mapping, got " + where.ToString(Formatting.None), i, node);
				JToken index = raw["index"];
				if (index != null && index.Type != JTokenType.Null && index.Type != JTokenType.Integer && index.Type != JTokenType.String)
					throw new PatchError("bad_index", "index must be an int or 'a:b', got " + index.ToString(Formatting.None), i, node);
				result.Add(new Override
				{
					Index = i,
					Node = node,
					Op = ops[0],
					Arg = raw[ops[0]],
					IndexToken = index != null && index.Type != JTokenType.Null ? index : null,
					Where = where as JObject,
				});
			}
			return result;
		}

		// ---- node resolution ----

		private static NodeSpec Resolve(string node)
		{
			int dot = node.IndexOf('.');
			if (dot <= 0 || !Roots.TryGetValue(node.Substring(0, dot), out var root)) return null;
			string[] segments = node.Substring(dot + 1).Split('.');
			if (segments.Any(s => s.Length == 0)) return null;
			var spec = new NodeSpec { Node = node, Root = node.Substring(0, dot), Table = root.Table, OwnerType = root.Type, RowType = root.Type };
			int[] lists = Enumerable.Range(0, segments.Length).Where(k => segments[k].EndsWith("[]", StringComparison.Ordinal)).ToArray();
			string[] leaf = segments;
			if (lists.Length > 1 || (lists.Length == 1 && spec.Table == Table.Spawners)) return null;
			if (lists.Length == 1)
			{
				int k = lists[0];
				spec.ListPath = segments.Take(k + 1).Select(s => s.EndsWith("[]", StringComparison.Ordinal) ? s.Substring(0, s.Length - 2) : s).ToArray();
				Type listType = PathType(spec.OwnerType, spec.ListPath);
				Type element = listType != null ? ElementType(listType) : null;
				if (element == null) return null;
				spec.RowType = element;
				spec.Table = spec.Table == Table.Single ? Table.List : Table.AssetList;
				leaf = segments.Skip(k + 1).ToArray();
			}
			if (leaf.Length == 0) return null;
			if (spec.Table == Table.Spawners && leaf.Length == 1 && TryStatName(leaf[0], out StatType stat))
			{
				spec.Stat = stat;
				spec.Kind = Kind.Double;
				spec.ValueType = typeof(double);
				return spec;
			}
			Type type = PathType(spec.RowType, leaf);
			if (type == null) return null;
			spec.LeafPath = leaf;
			Type element2 = ElementType(type);
			if (element2 != null)
			{
				spec.IsArray = true;
				type = element2;
			}
			spec.Kind = KindOf(type);
			if (spec.Kind == Kind.Other) return null;
			spec.ValueType = type;
			spec.IsPointList = spec.RowType == typeof(BalanceInterpolation) && leaf.Length == 1 && PointLists.Contains(leaf[0]);
			return spec;
		}

		private static bool TryStatName(string segment, out StatType stat)
		{
			stat = default;
			string name = null;
			if (segment.StartsWith("Stats[", StringComparison.Ordinal) && segment.EndsWith("]", StringComparison.Ordinal))
				name = segment.Substring(6, segment.Length - 7);
			else if (segment.StartsWith("stat.", StringComparison.Ordinal)) name = segment.Substring(5);
			if (name == null || !Enum.GetNames(typeof(StatType)).Contains(name)) return false;
			stat = (StatType)Enum.Parse(typeof(StatType), name);
			return true;
		}

		// Type at the end of a field path; null when a segment is not a field (lists only at the end).
		private static Type PathType(Type type, IList<string> path)
		{
			for (int i = 0; i < path.Count; i++)
			{
				if (type == null || (i > 0 && ElementType(type) != null)) return null;
				FieldInfo f = FindField(type, path[i]);
				if (f == null) return null;
				type = f.FieldType;
			}
			return type;
		}

		private static FieldInfo FindField(Type type, string name)
		{
			for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
			{
				FieldInfo f = t.GetField(name, DeclaredFields);
				if (f != null) return f;
			}
			return null;
		}

		private static Type ElementType(Type type)
		{
			if (type.IsArray) return type.GetElementType();
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
			return null;
		}

		private static Kind KindOf(Type t)
		{
			if (t == typeof(float)) return Kind.Float;
			if (t == typeof(double)) return Kind.Double;
			if (t == typeof(int)) return Kind.Int;
			if (t == typeof(bool)) return Kind.Bool;
			if (t == typeof(string)) return Kind.String;
			if (t.IsEnum) return Kind.Enum;
			if (typeof(Object).IsAssignableFrom(t)) return Kind.Ref;
			return Kind.Other;
		}

		// ---- rows ----

		private List<Row> Rows(NodeSpec spec)
		{
			string key = spec.OwnerType.FullName + "|" + spec.Table + "|" + string.Join(".", spec.ListPath);
			if (rowCache.TryGetValue(key, out List<Row> cached)) return cached;
			var rows = new List<Row>();
			switch (spec.Table)
			{
				case Table.Single:
				{
					object owner = Instance(spec.OwnerType);
					rows.Add(new Row { Id = new JObject(), Owner = owner, OwnerKey = OwnerKey(owner), Item = owner });
					break;
				}
				case Table.List:
				{
					object owner = Instance(spec.OwnerType);
					AddListRows(rows, owner, OwnerKey(owner), spec, null, null);
					break;
				}
				case Table.Assets:
				case Table.AssetList:
					foreach (var (path, asset) in Assets(spec.OwnerType))
					{
						string rel = AssetRow(path);
						if (spec.Table == Table.Assets)
							rows.Add(new Row { Id = new JObject { ["asset"] = rel }, Owner = asset, OwnerKey = path, Item = asset, AssetPath = rel, AssetName = asset.name });
						else AddListRows(rows, asset, path, spec, rel, asset.name);
					}
					break;
				case Table.Spawners:
					foreach (EnemySpawner s in Resources.FindObjectsOfTypeAll<EnemySpawner>())
					{
						if (s == null || !s.gameObject.scene.IsValid() || s.Config == null) continue;
						string runtime = SimulationHarness.HierarchyPath(s.transform) + "|" + s.Config.Key;
						JObject id = SpawnerIds().TryGetValue(runtime, out var sid)
							? new JObject { ["scene"] = sid.Scene, ["file_id"] = sid.FileId }
							: new JObject { ["runtime_id"] = runtime };
						rows.Add(new Row { Id = id, Owner = s.Config, OwnerKey = "spawner:" + runtime, Item = s.Config, Spawner = s });
					}
					break;
			}
			foreach (Row r in rows) r.IdKey = r.Id.ToString(Formatting.None);
			rows.Sort((a, b) => string.CompareOrdinal(a.IdKey, b.IdKey));
			rowCache[key] = rows;
			return rows;
		}

		private static void AddListRows(List<Row> rows, object owner, string ownerKey, NodeSpec spec, string asset, string assetName)
		{
			if (!(GetPath(owner, spec.ListPath) is IList list)) throw new PatchError("bad_patch", ownerKey + "." + string.Join(".", spec.ListPath) + " is null", null, spec.Node);
			for (int i = 0; i < list.Count; i++)
			{
				var id = new JObject();
				if (asset != null) id["asset"] = asset;
				id["idx"] = i;
				rows.Add(new Row { Id = id, Owner = owner, OwnerKey = ownerKey, List = list, Index = i, AssetPath = asset, AssetName = assetName });
			}
		}

		private static object Instance(Type type)
		{
			object instance = null;
			if (type == typeof(SimulationConfig))
			{
				if (SimulationSystem.Instance != null)
					instance = typeof(SimulationSystem).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SimulationSystem.Instance);
			}
			else if ((bool)typeof(GameContext).GetMethod("Has").MakeGenericMethod(type).Invoke(null, null))
				instance = typeof(GameContext).GetMethod("Get").MakeGenericMethod(type).Invoke(null, null);
			if (instance == null) throw new PatchError("bad_patch", "no live " + type.Name);
			return instance;
		}

		private static string OwnerKey(object owner)
		{
#if UNITY_EDITOR
			if (owner is Object o)
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(o);
				if (!string.IsNullOrEmpty(path)) return path;
			}
#endif
			return owner is Object named ? owner.GetType().Name + ":" + named.name : owner.GetType().Name;
		}

		private static string AssetRow(string path) => path.StartsWith("Assets/", StringComparison.Ordinal) ? path.Substring(7) : path;

		// Every asset of the type (the extractor's rows), sorted by path; the editor's AssetDatabase hands out the
		// same objects the game holds.
		private List<(string Path, Object Asset)> Assets(Type type)
		{
			if (assetCache.TryGetValue(type, out var cached)) return cached;
			var result = new List<(string, Object)>();
#if UNITY_EDITOR
			foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:" + type.Name))
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
				if (asset != null && asset.GetType() == type) result.Add((path, asset));
			}
#else
			throw new PatchError("bad_patch", type.Name + " rows need the editor's AssetDatabase");
#endif
			result.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
			assetCache[type] = result;
			return result;
		}

		private Dictionary<string, (string Scene, long FileId)> SpawnerIds()
		{
			if (spawnerIds != null) return spawnerIds;
			SimulationRunRequest.PatchSpawnerMap map = job.SpawnerMap;
			if (map == null) throw new PatchError("bad_patch", "spawner nodes need patch_spawner_map (C1's identity map)");
			string path = Path.IsPathRooted(map.Path) ? map.Path : Path.Combine(request.OutDir, map.Path);
			byte[] bytes = File.ReadAllBytes(path);
			string sha = SimulationRuntimeProbes.Sha256Hex(bytes);
			if (sha != map.Sha256) throw new PatchError("bad_patch", "patch_spawner_map " + path + " has sha256 " + sha + ", the request expects " + map.Sha256);
			JObject root = SimulationCanonicalJson.Parse(Encoding.UTF8.GetString(bytes));
			var ids = new Dictionary<string, (string, long)>(StringComparer.Ordinal);
			foreach (JToken e in (JArray)root["spawners"])
			{
				string runtime = (string)e["path"] + "|" + (string)e["key"];
				if (ids.ContainsKey(runtime)) throw new PatchError("bad_patch", "patch_spawner_map: runtime id " + runtime + " is not unique");
				ids[runtime] = ((string)e["scene"], (long)e["file_id"]);
			}
			spawnerIds = ids;
			return ids;
		}

		// ---- applying one override (model.patch._apply_override) ----

		private JObject ApplyOverride(Override ov, NodeSpec spec)
		{
			List<Row> rows = Rows(spec);
			List<Row> selected = SelectRows(spec, rows, ov);
			int leaves = 0;
			foreach (Row row in selected)
			{
				if (row.Id["runtime_id"] != null) unmappedSpawners.Add((string)row.Id["runtime_id"]);
				Touch(spec, row);
				if (!spec.IsArray)
				{
					object old = ReadLeaf(spec, row);
					object value = ov.Op == "set" ? Coerce(spec, ov.Arg, ov) : Arith(spec, ov.Op, old, ov.Arg, ov);
					WriteLeaf(spec, row, value, ov);
					leaves++;
					continue;
				}
				List<object> current = ReadArray(spec, row);
				List<object> next;
				if (ov.IndexToken == null && ov.Op == "set")
				{
					if (!(ov.Arg is JArray values)) throw Bad("bad_type", "set on a whole array needs a list", ov);
					next = values.Select(v => Coerce(spec, v, ov)).ToList();
					leaves += Math.Max(current.Count, next.Count);
				}
				else
				{
					if (ov.Arg is JArray) throw Bad("bad_type", "a list value needs set without index", ov);
					IEnumerable<int> positions = ov.IndexToken == null ? Enumerable.Range(0, current.Count) : ParseIndex(ov.IndexToken, current.Count, ov);
					next = new List<object>(current);
					foreach (int k in positions)
					{
						next[k] = ov.Op == "set" ? Coerce(spec, ov.Arg, ov) : Arith(spec, ov.Op, current[k], ov.Arg, ov);
						leaves++;
					}
				}
				WriteArray(spec, row, next);
			}
			if (spec.Root == "ABTestDataConfig") abTests.Add(spec.ListPath.Length > 0 ? spec.ListPath[0] : spec.LeafPath[0]);
			return new JObject { ["override"] = ov.Index, ["node"] = ov.Node, ["op"] = ov.Op, ["rows"] = selected.Count, ["leaves"] = leaves };
		}

		private static PatchError Bad(string code, string message, Override ov) => new PatchError(code, message, ov.Index, ov.Node);

		private List<Row> SelectRows(NodeSpec spec, List<Row> rows, Override ov)
		{
			if (spec.Table == Table.Single)
			{
				if (ov.Where != null && ov.Where.Count > 0) throw Bad("bad_selector", "a single-asset config takes no where", ov);
				if (ov.IndexToken != null && !spec.IsArray) throw Bad("bad_index", "index on a scalar field of a single-asset config", ov);
				return rows;
			}
			bool rowIndex = ov.IndexToken != null && !spec.IsArray;
			if (ov.Where == null && !rowIndex)
				throw Bad("bad_selector", spec.Node + " has " + rows.Count + " rows: give where (where: {} for all)" + (spec.Table == Table.List ? " or index" : ""), ov);
			IEnumerable<Row> selected = rows;
			if (rowIndex)
			{
				if (spec.Table != Table.List) throw Bad("bad_index", spec.Node + " rows are not a list; use where", ov);
				var positions = new HashSet<int>(ParseIndex(ov.IndexToken, rows.Count, ov));
				selected = rows.Where(r => positions.Contains(r.Index));
			}
			List<Row> result = selected.ToList();
			if (ov.Where != null)
			{
				foreach (JProperty p in ov.Where.Properties())
				{
					if (rows.Count > 0 && !TryWhereValue(spec, rows[0], p.Name, out _, out _, out _))
						throw Bad("bad_where", spec.Node + " has no field " + p.Name, ov);
					result = result.Where(r => TryWhereValue(spec, r, p.Name, out object cell, out Kind kind, out Type type) && Matches(cell, kind, type, p.Value)).ToList();
				}
			}
			if (result.Count == 0) throw Bad("no_match", "selector " + Describe(ov) + " matches no " + spec.Node + " row", ov);
			return result;
		}

		private static string Describe(Override ov) =>
			"{" + (ov.Where != null ? "where: " + ov.Where.ToString(Formatting.None) : "") + (ov.IndexToken != null ? " index: " + ov.IndexToken.ToString(Formatting.None) : "") + "}";

		// model.patch._parse_index: an int, or "a:b" half-open ("a:" to the end).
		private static List<int> ParseIndex(JToken index, int n, Override ov)
		{
			if (index.Type == JTokenType.Integer)
			{
				BigInteger k = ToBig(index);
				if (k < 0 || k >= n) throw Bad("bad_index", "index " + k + " out of range 0.." + (n - 1), ov);
				return new List<int> { (int)k };
			}
			string text = ((string)index).Trim();
			string digits = text.TrimStart('-');
			if (digits.Length > 0 && digits.All(char.IsDigit))
			{
				if (!BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out BigInteger k) || k < 0 || k >= n)
					throw Bad("bad_index", "index " + text + " out of range 0.." + (n - 1), ov);
				return new List<int> { (int)k };
			}
			int colon = text.IndexOf(':');
			string a = colon >= 0 ? text.Substring(0, colon) : text, b = colon >= 0 ? text.Substring(colon + 1) : "";
			if (!TryPyInt(a, 0, out long lo) || !TryPyInt(b, n, out long hi))
				throw Bad("bad_index", "index '" + index + "' is not an int or 'a:b'", ov);
			if (colon < 0 || !(0 <= lo && lo < hi && hi <= n)) throw Bad("bad_index", "index '" + index + "' outside 0.." + n, ov);
			return Enumerable.Range((int)lo, (int)(hi - lo)).ToList();
		}

		// Python's int() of a slice bound; blank = the default.
		private static bool TryPyInt(string text, long blank, out long value)
		{
			value = blank;
			if (text.Trim().Length == 0) return true;
			return long.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
		}

		// ---- where (model.patch._matches, typed by the live field) ----

		private bool TryWhereValue(NodeSpec spec, Row row, string name, out object value, out Kind kind, out Type type)
		{
			value = null;
			kind = Kind.String;
			type = typeof(string);
			if ((spec.Table == Table.List || spec.Table == Table.AssetList) && name == "idx")
			{
				value = (long)row.Index;
				kind = Kind.Int;
				type = typeof(int);
				return true;
			}
			if (spec.Table == Table.Assets || spec.Table == Table.AssetList)
			{
				if (name == "asset")
				{
					value = row.AssetPath;
					return true;
				}
				if (name == "asset_name")
				{
					value = row.AssetName;
					return true;
				}
			}
			if (spec.Table == Table.Spawners)
			{
				switch (name)
				{
					case "scene":
						value = (string)row.Id["scene"];
						return true;
					case "file_id":
						value = row.Id["file_id"] != null ? (object)(long)row.Id["file_id"] : null;
						kind = Kind.Int;
						type = typeof(int);
						return true;
					case "go_name":
						value = row.Spawner.gameObject.name;
						return true;
					case "go_path":
						string path = SimulationHarness.HierarchyPath(row.Spawner.transform);
						value = path.Substring(path.IndexOf(':') + 1);
						return true;
				}
				if (TryStatName(name, out StatType stat))
				{
					value = FindStat(row, stat)?.BaseValue;
					kind = Kind.Double;
					type = typeof(double);
					return true;
				}
			}
			string[] path2 = name.Split('.');
			Type t = PathType(spec.RowType, path2);
			if (t == null) return false;
			type = t;
			kind = ElementType(t) != null ? Kind.Other : KindOf(t);
			value = GetPath(row.Get(), path2);
			return true;
		}

		private static bool Matches(object cell, Kind kind, Type type, JToken want)
		{
			if (want.Type == JTokenType.String)
			{
				string s = (string)want;
				if (kind == Kind.Enum) return cell != null && Enum.GetName(type, cell) == s;
				if (kind == Kind.Float || kind == Kind.Double) return false;
			}
			if (want.Type == JTokenType.Null || cell == null) return want.Type == JTokenType.Null && cell == null;
			if (IsNumber(want) || want.Type == JTokenType.Boolean)
			{
				double w = want.Type == JTokenType.Boolean ? ((bool)want ? 1.0 : 0.0) : Num(want);
				switch (kind)
				{
					case Kind.Float: return (float)w == (float)cell;
					case Kind.Double: return (double)cell == w;
					case Kind.Int: return Convert.ToDouble(cell, CultureInfo.InvariantCulture) == w;
					case Kind.Bool: return ((bool)cell ? 1.0 : 0.0) == w;
					case Kind.Enum: return Convert.ToInt64(cell, CultureInfo.InvariantCulture) == w;
					case Kind.String:
					case Kind.Ref:
						string text = kind == Kind.Ref ? RefName(cell) : (string)cell;
						return text != null && double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d == w;
					default: return false;
				}
			}
			if (want.Type != JTokenType.String) return false;
			string wanted = (string)want;
			switch (kind)
			{
				case Kind.String: return (string)cell == wanted;
				case Kind.Ref: return RefName(cell) == wanted;
				case Kind.Int: return Convert.ToInt64(cell, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) == wanted;
				case Kind.Bool: return ((bool)cell ? "1" : "0") == wanted;
				default: return false;
			}
		}

		// ---- values (model.patch._coerce, _arith) ----

		private object Coerce(NodeSpec spec, JToken value, Override ov)
		{
			string node = spec.Node;
			switch (spec.Kind)
			{
				case Kind.Float:
				case Kind.Double:
				{
					if (!IsNumber(value) || !IsFinite(Num(value))) throw Bad("bad_type", spec.Kind.ToString().ToLowerInvariant() + " field needs a finite number, got " + Text(value), ov);
					double d = Num(value);
					if (spec.Kind == Kind.Double) return d;
					float f = (float)d;
					if (!IsFinite(f)) throw Bad("bad_type", Text(value) + " gives " + f.ToString(CultureInfo.InvariantCulture) + " as a float", ov);
					return f;
				}
				case Kind.Int:
				{
					if (!IsNumber(value) || !Integral(value)) throw Bad("bad_type", "int field needs an integer, got " + Text(value), ov);
					BigInteger v = value.Type == JTokenType.Integer ? ToBig(value) : new BigInteger(Num(value));
					if (v < int.MinValue || v > int.MaxValue) throw Bad("bad_type", v + " is outside int32", ov);
					return (int)v;
				}
				case Kind.Bool:
					if (value.Type == JTokenType.Boolean) return (bool)value;
					if (IsNumber(value) && (Num(value) == 0.0 || Num(value) == 1.0)) return Num(value) == 1.0;
					throw Bad("bad_type", "bool field needs true/false, got " + Text(value), ov);
				case Kind.Enum:
				{
					if (value.Type == JTokenType.String)
					{
						string name = (string)value;
						if (Enum.GetNames(spec.ValueType).Contains(name)) return Enum.Parse(spec.ValueType, name);
						throw Bad("bad_type", Text(value) + " is not a " + spec.ValueType.Name, ov);
					}
					if (!IsNumber(value) || !Integral(value)) throw Bad("bad_type", "enum field needs a member name or an integer, got " + Text(value), ov);
					BigInteger v = value.Type == JTokenType.Integer ? ToBig(value) : new BigInteger(Num(value));
					object member = v >= long.MinValue && v <= long.MaxValue ? Enum.ToObject(spec.ValueType, (long)v) : null;
					if (member == null || !Enum.IsDefined(spec.ValueType, member)) throw Bad("bad_type", Text(value) + " is not a " + spec.ValueType.Name, ov);
					return member;
				}
				default:
				{
					if (value.Type != JTokenType.Null && value.Type != JTokenType.String) throw Bad("bad_type", spec.Kind.ToString().ToLowerInvariant() + " field needs a string, got " + Text(value), ov);
					string s = value.Type == JTokenType.Null ? null : (string)value;
					if (spec.Kind == Kind.Ref)
					{
						if (s == null) return null;
						var match = Assets(spec.ValueType).FirstOrDefault(a => a.Asset.name == s);
						if (match.Asset == null) throw Bad("bad_ref", "no asset named " + Text(value) + " for " + spec.ValueType.Name, ov);
						return match.Asset;
					}
					if (spec.IsPointList && s != null && !s.Split(';').All(x => double.TryParse(x.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && IsFinite(d)))
						throw Bad("bad_type", node + " needs ';'-separated finite numbers, got " + Text(value), ov);
					return s;
				}
			}
		}

		private static object Arith(NodeSpec spec, string op, object old, JToken arg, Override ov)
		{
			if (spec.Kind != Kind.Float && spec.Kind != Kind.Double && spec.Kind != Kind.Int)
				throw Bad("bad_op", op + " needs a numeric field, " + spec.Node + " is " + spec.Kind.ToString().ToLowerInvariant(), ov);
			if (!IsNumber(arg) || !IsFinite(Num(arg))) throw Bad("bad_type", op + " needs a finite number, got " + Text(arg), ov);
			if (old == null) throw Bad("missing_value", op + " on a missing value", ov);
			double a = Num(arg);
			double o = Convert.ToDouble(old, CultureInfo.InvariantCulture);
			double r = op == "add" ? o + a : o * a;
			switch (spec.Kind)
			{
				case Kind.Float:
					float f = (float)r;
					if (IsFinite(o) && !IsFinite(f)) throw Bad("bad_type", op + " " + Text(arg) + " gives " + f.ToString(CultureInfo.InvariantCulture) + " as a float", ov);
					return f;
				case Kind.Double:
					if (IsFinite(o) && !IsFinite(r)) throw Bad("bad_type", op + " " + Text(arg) + " gives " + r.ToString(CultureInfo.InvariantCulture) + " as a double", ov);
					return r;
				default:
					if (!IsFinite(r) || r != Math.Floor(r) || r < int.MinValue || r > int.MaxValue)
						throw Bad("bad_type", "int field result " + r.ToString("R", CultureInfo.InvariantCulture) + " is not an int32", ov);
					return (int)r;
			}
		}

		private static bool IsNumber(JToken t) => t != null && (t.Type == JTokenType.Integer || t.Type == JTokenType.Float);

		private static double Num(JToken t)
		{
			object v = ((JValue)t).Value;
			switch (v)
			{
				case double d: return d;
				case long l: return l;
				case int i: return i;
				case BigInteger b: return (double)b;
				case float f: return f;
				default: return Convert.ToDouble(v, CultureInfo.InvariantCulture);
			}
		}

		private static BigInteger ToBig(JToken t)
		{
			object v = ((JValue)t).Value;
			return v is BigInteger b ? b : new BigInteger(Convert.ToInt64(v, CultureInfo.InvariantCulture));
		}

		private static bool Integral(JToken t) => t.Type == JTokenType.Integer || (IsFinite(Num(t)) && Num(t) == Math.Floor(Num(t)));

		private static bool IsFinite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);

		private static string Text(JToken t) => t == null ? "null" : t.ToString(Formatting.None);

		// ---- reading and writing leaves ----

		private static Stat FindStat(Row row, StatType type)
		{
			List<Stat> stats = ((EnemySpawnerConfig)row.Item).Stats;
			return stats?.FirstOrDefault(s => s != null && s.Type == type);
		}

		private static object ReadLeaf(NodeSpec spec, Row row)
		{
			if (spec.Stat.HasValue) return FindStat(row, spec.Stat.Value)?.BaseValue;
			return GetPath(row.Get(), spec.LeafPath);
		}

		private void WriteLeaf(NodeSpec spec, Row row, object value, Override ov)
		{
			if (spec.Stat.HasValue)
			{
				Stat stat = FindStat(row, spec.Stat.Value);
				if (stat == null) throw Bad("missing_value", row.IdKey + " has no " + spec.Stat.Value + " stat", ov);
				stat.BaseValue = (double)value;
				return;
			}
			object item = row.Get();
			SetPath(item, spec.LeafPath, value);
			row.Put(item);
			AfterWrite(spec, row);
		}

		private static List<object> ReadArray(NodeSpec spec, Row row)
		{
			object container = GetPath(row.Get(), spec.LeafPath);
			return container is IList list ? list.Cast<object>().ToList() : new List<object>();
		}

		private void WriteArray(NodeSpec spec, Row row, List<object> values)
		{
			object item = row.Get();
			object container = GetPath(item, spec.LeafPath);
			if (container is Array array && array.Length == values.Count)
			{
				for (int i = 0; i < values.Count; i++) array.SetValue(values[i], i);
			}
			else if (container is Array || container == null)
			{
				Type fieldType = PathType(spec.RowType, spec.LeafPath);
				object fresh;
				if (fieldType.IsArray)
				{
					Array a = Array.CreateInstance(spec.ValueType, values.Count);
					for (int i = 0; i < values.Count; i++) a.SetValue(values[i], i);
					fresh = a;
				}
				else
				{
					var l = (IList)Activator.CreateInstance(fieldType);
					foreach (object v in values) l.Add(v);
					fresh = l;
				}
				SetPath(item, spec.LeafPath, fresh);
				row.Put(item);
			}
			else
			{
				var list = (IList)container;
				if (list.Count == values.Count)
				{
					for (int i = 0; i < values.Count; i++) list[i] = values[i];
				}
				else
				{
					list.Clear();
					foreach (object v in values) list.Add(v);
				}
			}
			AfterWrite(spec, row);
		}

		// keyData/valueData of a UnitySerializedDictionary feed the dictionary only on deserialization: rebuild it.
		private static void AfterWrite(NodeSpec spec, Row row)
		{
			if (spec.LeafPath == null || spec.LeafPath.Length < 2) return;
			object parent = GetPath(row.Get(), spec.LeafPath.Take(spec.LeafPath.Length - 1).ToArray());
			if (parent is ISerializationCallbackReceiver receiver && IsSerializedDictionary(parent.GetType())) receiver.OnAfterDeserialize();
		}

		private static bool IsSerializedDictionary(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(UnitySerializedDictionary<,>)) return true;
			return false;
		}

		private static object GetPath(object value, IList<string> path)
		{
			foreach (string name in path)
			{
				if (value == null) return null;
				FieldInfo f = FindField(value.GetType(), name);
				if (f == null) throw new PatchError("bad_patch", value.GetType().Name + " has no field " + name);
				value = f.GetValue(value);
			}
			return value;
		}

		// Sets the field at the end of the path; struct fields on the way are written back.
		private static void SetPath(object value, IList<string> path, object leaf, int k = 0)
		{
			if (value == null) throw new PatchError("bad_patch", "null object before field " + path[k]);
			FieldInfo f = FindField(value.GetType(), path[k]);
			if (k == path.Count - 1)
			{
				f.SetValue(value, leaf);
				return;
			}
			object child = f.GetValue(value);
			SetPath(child, path, leaf, k + 1);
			if (f.FieldType.IsValueType) f.SetValue(value, child);
		}

		// First touch of a (node, row): remember the value for the net leaves and the undo, digest its owner.
		private void Touch(NodeSpec spec, Row row)
		{
			string key = spec.Node + "\n" + row.IdKey;
			if (touched.ContainsKey(key)) return;
			if (!owners.ContainsKey(row.OwnerKey)) owners[row.OwnerKey] = new OwnerState { Owner = row.Owner, Before = Digest(row.Owner) };
			var t = new Touched { Spec = spec, Row = row };
			if (!spec.IsArray)
			{
				object old = ReadLeaf(spec, row);
				t.Old = Normalize(spec.Kind, old);
				if (spec.Stat.HasValue)
				{
					Stat stat = FindStat(row, spec.Stat.Value);
					if (stat != null) undo.Add(() => stat.BaseValue = (double)old);
				}
				else undo.Add(() =>
				{
					object item = row.Get();
					SetPath(item, spec.LeafPath, old);
					row.Put(item);
					AfterWrite(spec, row);
				});
			}
			else
			{
				object container = GetPath(row.Get(), spec.LeafPath);
				List<object> contents = container is IList list ? list.Cast<object>().ToList() : null;
				t.Old = contents?.Select(v => Normalize(spec.Kind, v)).ToList();
				undo.Add(() =>
				{
					object item = row.Get();
					SetPath(item, spec.LeafPath, container);
					row.Put(item);
					if (container is IList l && contents != null)
					{
						if (l is Array a)
						{
							for (int i = 0; i < contents.Count; i++) a.SetValue(contents[i], i);
						}
						else
						{
							l.Clear();
							foreach (object v in contents) l.Add(v);
						}
					}
					AfterWrite(spec, row);
				});
			}
			touched[key] = t;
			touchOrder.Add(t);
		}

		private static object Normalize(Kind kind, object value)
		{
			if (value == null) return null;
			switch (kind)
			{
				case Kind.Float: return (double)(float)value;
				case Kind.Double: return (double)value;
				case Kind.Int:
				case Kind.Enum: return Convert.ToInt64(value, CultureInfo.InvariantCulture);
				case Kind.Ref: return RefName(value);
				default: return value;
			}
		}

		private static string RefName(object value) => value is Object o && o != null ? o.name : null;

		private static bool SameValue(object a, object b)
		{
			if (a == null || b == null) return a == null && b == null;
			if (a is double x && b is double y) return x == y;
			return a.GetType() == b.GetType() && a.Equals(b);
		}

		// model.patch._net_leaves over the touched (node, row) pairs, values read back from the live objects.
		private JArray NetLeaves(out int verified)
		{
			var leaves = new List<(string Node, string Row, int Index, JObject Leaf)>();
			verified = 0;
			foreach (Touched t in touchOrder)
			{
				NodeSpec spec = t.Spec;
				if (!spec.IsArray)
				{
					object now = Normalize(spec.Kind, ReadLeaf(spec, t.Row));
					verified++;
					if (!SameValue(t.Old, now)) leaves.Add((spec.Node, t.Row.IdKey, -1, Leaf(spec, t.Row, null, t.Old, now)));
					continue;
				}
				var before = (List<object>)t.Old ?? new List<object>();
				List<object> after = ReadArray(spec, t.Row).Select(v => Normalize(spec.Kind, v)).ToList();
				for (int k = 0; k < Math.Max(before.Count, after.Count); k++)
				{
					object a = k < before.Count ? before[k] : null, b = k < after.Count ? after[k] : null;
					verified++;
					if (!SameValue(a, b)) leaves.Add((spec.Node, t.Row.IdKey, k, Leaf(spec, t.Row, k, a, b)));
				}
			}
			var result = new JArray();
			foreach (var x in leaves.OrderBy(l => l.Node, StringComparer.Ordinal).ThenBy(l => l.Row, StringComparer.Ordinal).ThenBy(l => l.Index))
				result.Add(x.Leaf);
			return result;
		}

		private static JObject Leaf(NodeSpec spec, Row row, int? index, object old, object now) => new JObject
		{
			["node"] = spec.Node,
			["row"] = row.Id.DeepClone(),
			["index"] = index.HasValue ? (JToken)index.Value : JValue.CreateNull(),
			["old"] = Value(spec.Kind, old),
			["new"] = Value(spec.Kind, now),
		};

		private static JToken Value(Kind kind, object value)
		{
			if (value == null) return JValue.CreateNull();
			switch (value)
			{
				case double d: return new JRaw(SimulationJson.JsonDouble(d));
				case long l: return new JValue(l);
				case bool b: return new JValue(b);
				case string s: return new JValue(s);
				default: return new JValue(value.ToString());
			}
		}

		// ---- AB data: the asset's JSON round trip (ABTestInitializer.ApplyDevelopTests) ----

		private JObject AbRoundTrip()
		{
			var config = (ABTestDataConfig)Instance(typeof(ABTestDataConfig));
			var tests = new JObject();
			var groups = new Dictionary<string, object>();
			foreach (string field in abTests)
			{
				FieldInfo g = typeof(ABTestData).GetField(char.ToUpperInvariant(field[0]) + field.Substring(1), BindingFlags.Public | BindingFlags.Static);
				if (g == null) throw new PatchError("bad_patch", "ABTestData has no group for " + field);
				groups[field] = g.GetValue(null);
				tests[field] = new JObject { ["version_before"] = Version(groups[field]) };
			}
			ABTestData.ApplyRemoteAll(config.BuildDataJson(), null, false);
			foreach (string field in abTests)
			{
				object group = groups[field];
				var entry = (JObject)tests[field];
				entry["version_after"] = Version(group);
				if ((int)entry["version_after"] == (int)entry["version_before"])
					throw new PatchError("bad_patch", "the AB data round trip did not reach " + field + " (see the log)");
				object current = group.GetType().GetMethod("GetCurrentValue").Invoke(group, null);
				entry["effective"] = current != null ? SimulationJson.ToJObject(current) : null;
			}
			return new JObject { ["develop_cohorts_mode"] = config.developCohortsMode.ToString(), ["tests"] = tests };
		}

		private static int Version(object group) => (int)group.GetType().GetProperty("Version").GetValue(group);

		// ---- digest of a patched object (what Unity serializes, plus dictionary entries) ----

		private static string Digest(object owner)
		{
			var sb = new StringBuilder();
			WalkFields(owner, owner.GetType(), sb, new HashSet<object>(ReferenceComparer.Instance), 0);
			using (SHA256 sha = SHA256.Create())
				return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())).Select(b => b.ToString("x2")));
		}

		private static void Walk(object value, StringBuilder sb, HashSet<object> seen, int depth)
		{
			switch (value)
			{
				case null: sb.Append("~;"); return;
				case double d: sb.Append(BitConverter.DoubleToInt64Bits(d)).Append(';'); return;
				case float f: sb.Append(BitConverter.SingleToInt32Bits(f)).Append(';'); return;
				case string s: sb.Append(s.Length).Append(':').Append(s).Append(';'); return;
				// Instance ids stay readable while Play ends and scene objects go away.
				case Object o: sb.Append("obj:").Append(o.GetEntityId().ToString()).Append(';'); return;
			}
			Type t = value.GetType();
			if (t.IsPrimitive || t.IsEnum || value is decimal)
			{
				sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append(';');
				return;
			}
			if (depth > 24 || (!t.IsValueType && !seen.Add(value)))
			{
				sb.Append("ref;");
				return;
			}
			if (value is IDictionary dict)
			{
				sb.Append('{').Append(dict.Count).Append(':');
				foreach (DictionaryEntry e in dict)
				{
					Walk(e.Key, sb, seen, depth + 1);
					Walk(e.Value, sb, seen, depth + 1);
				}
				sb.Append('}');
			}
			if (value is IList list)
			{
				sb.Append('[').Append(list.Count).Append(':');
				foreach (object item in list) Walk(item, sb, seen, depth + 1);
				sb.Append(']');
				return;
			}
			WalkFields(value, t, sb, seen, depth + 1);
		}

		private static void WalkFields(object value, Type type, StringBuilder sb, HashSet<object> seen, int depth)
		{
			sb.Append('(');
			for (Type t = type; t != null && t != typeof(object) && t != typeof(ScriptableObject) && t != typeof(Object) && t != typeof(MonoBehaviour); t = t.BaseType)
			{
				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) break;
				foreach (FieldInfo f in t.GetFields(DeclaredFields).OrderBy(f => f.Name, StringComparer.Ordinal))
				{
					bool serialized = (f.IsPublic && !f.IsNotSerialized) || f.IsDefined(typeof(SerializeField), false);
					if (!serialized || typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
					if (f.FieldType.Namespace != null && f.FieldType.Namespace.StartsWith("UniMob", StringComparison.Ordinal)) continue;
					sb.Append(f.Name).Append('=');
					Walk(f.GetValue(value), sb, seen, depth);
				}
			}
			sb.Append(')');
		}

		private sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceComparer Instance = new ReferenceComparer();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}
	}
}
