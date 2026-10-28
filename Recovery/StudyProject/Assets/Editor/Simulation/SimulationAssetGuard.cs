using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Codebase.Simulation.EditorTools
{
	// Protected config assets (design 2.3): Play mode may write into them in memory (OnAwake rewrites
	// Simulation Config; PlayerModel hands PlayerConfig modifiers to stats by reference), and such writes
	// survive leaving Play. The guard keeps a baseline of the in-memory digests taken before the first
	// Play, checks after each job, forces a reimport of any asset that differs and fails (exit 2) when
	// that does not restore it. It never saves assets.
	public static class SimulationAssetGuard
	{
		public static readonly string[] ProtectedAssets =
		{
			"Assets/MonoBehaviour/Simulation Config.asset",
			"Assets/MonoBehaviour/Simulation Data.asset",
			"Assets/MonoBehaviour/Combat Balance Config.asset",
			"Assets/MonoBehaviour/Player Config.asset",
			"Assets/MonoBehaviour/ABTest Data Config.asset",
			"Assets/MonoBehaviour/Locations Config.asset",
			"Assets/Simulation/Simulation Rebuild Params.asset",
		};

		// Asset path -> sha256 of EditorJsonUtility.ToJson of every object in the file; missing assets
		// (the params asset before C3c) are skipped.
		public static Dictionary<string, string> MemoryDigests()
		{
			var result = new Dictionary<string, string>();
			foreach (string path in ProtectedAssets)
			{
				string digest = MemoryDigest(path);
				if (digest != null) result[path] = digest;
			}
			return result;
		}

		public static string MemoryDigest(string path)
		{
			if (!File.Exists(Path.Combine(ProjectDir, path))) return null;
			UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
			if (objects == null || objects.Length == 0) return null;
			var sb = new StringBuilder();
			foreach (UnityEngine.Object o in objects.Where(o => o != null).OrderBy(o => o.GetType().FullName, StringComparer.Ordinal).ThenBy(o => o.name, StringComparer.Ordinal))
			{
				sb.Append(o.GetType().FullName).Append('\n').Append(EditorJsonUtility.ToJson(o)).Append('\n');
				if (o is ScriptableObject) WalkFields(o, o.GetType(), sb, new HashSet<object>(ReferenceComparer.Instance), 0);
				sb.Append('\n');
			}
			return Sha256(Encoding.UTF8.GetBytes(sb.ToString()));
		}

		// Managed state Unity does not serialize, e.g. StatBase.Modifiers (an auto-property): PlayerModel
		// shares PlayerConfig.Stats[i].Modifiers with live stats, so rewards land in the asset object, and
		// EditorJsonUtility cannot see them. Walks primitive, string, list and [Serializable] members; skips
		// UniMob atoms and their caches, delegates, pointers and dictionaries.
		private static void Walk(object value, StringBuilder sb, HashSet<object> seen, int depth)
		{
			if (value == null)
			{
				sb.Append("~;");
				return;
			}
			Type t = value.GetType();
			switch (value)
			{
				case double d: sb.Append(BitConverter.DoubleToInt64Bits(d)).Append(';'); return;
				case float f: sb.Append(BitConverter.SingleToInt32Bits(f)).Append(';'); return;
				case string str: sb.Append(str.Length).Append(':').Append(str).Append(';'); return;
				case UnityEngine.Object o: sb.Append("obj:").Append(o != null ? o.name : "null").Append(';'); return;
			}
			if (t.IsPrimitive || t.IsEnum || value is decimal)
			{
				sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)).Append(';');
				return;
			}
			if (depth > 40)
			{
				sb.Append("deep;");
				return;
			}
			if (!t.IsValueType && !seen.Add(value))
			{
				sb.Append("ref;");
				return;
			}
			if (value is IList list)
			{
				sb.Append('[').Append(list.Count).Append(':');
				foreach (object item in list) Walk(item, sb, seen, depth + 1);
				sb.Append(']');
				return;
			}
			if (!t.IsValueType && !t.IsSerializable)
			{
				sb.Append("skip;");
				return;
			}
			WalkFields(value, t, sb, seen, depth + 1);
		}

		private static void WalkFields(object value, Type type, StringBuilder sb, HashSet<object> seen, int depth)
		{
			sb.Append('{');
			for (Type t = type; t != null && t != typeof(object) && t != typeof(ScriptableObject) && t != typeof(UnityEngine.Object); t = t.BaseType)
			{
				foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(f => f.Name, StringComparer.Ordinal))
				{
					Type ft = f.FieldType;
					if (typeof(Delegate).IsAssignableFrom(ft) || ft == typeof(IntPtr) || ft == typeof(UIntPtr)) continue;
					if (f.Name.StartsWith("__", StringComparison.Ordinal) && f.Name.EndsWith("__BackingField", StringComparison.Ordinal)) continue;
					if (ft.Namespace != null && ft.Namespace.StartsWith("UniMob", StringComparison.Ordinal)) continue;
					if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Dictionary<,>)) continue;
					sb.Append(f.Name).Append('=');
					Walk(f.GetValue(value), sb, seen, depth);
				}
			}
			sb.Append('}');
		}

		private sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceComparer Instance = new ReferenceComparer();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}

		public static Dictionary<string, string> DiskHashes()
		{
			var result = new Dictionary<string, string>();
			foreach (string path in ProtectedAssets)
			{
				string full = Path.Combine(ProjectDir, path);
				if (File.Exists(full)) result[path] = Sha256(File.ReadAllBytes(full));
			}
			return result;
		}

		// Compares with the baseline and restores differing assets. Returns a report; ok = false when an
		// asset could not be restored or its file on disk changed.
		public static JObject CheckAndRestore(Dictionary<string, string> baseline, Dictionary<string, string> diskBaseline, bool allowReimport)
		{
			var changed = new JArray();
			var reimported = new JArray();
			var unloaded = new JArray();
			var failed = new JArray();
			var diskChanged = new JArray();
			var changedDigests = new JObject();
			foreach (var entry in baseline.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				string path = entry.Key;
				string digest = MemoryDigest(path);
				if (digest == entry.Value) continue;
				changed.Add(Name(path));
				changedDigests[Name(path)] = digest;
				if (!allowReimport)
				{
					failed.Add(Name(path));
					continue;
				}
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				if (MemoryDigest(path) == entry.Value)
				{
					reimported.Add(Name(path));
					continue;
				}
				// Fallback: drop the in-memory objects so the next load reads the file again.
				foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(path))
					if (o != null && !(o is GameObject) && !(o is Component)) Resources.UnloadAsset(o);
				if (MemoryDigest(path) == entry.Value) unloaded.Add(Name(path));
				else failed.Add(Name(path));
			}
			Dictionary<string, string> disk = DiskHashes();
			foreach (var entry in diskBaseline.OrderBy(x => x.Key, StringComparer.Ordinal))
				if (!disk.TryGetValue(entry.Key, out string now) || now != entry.Value) diskChanged.Add(Name(entry.Key));
			return new JObject
			{
				["ok"] = failed.Count == 0 && diskChanged.Count == 0,
				["changed_in_memory"] = changed,
				["changed_sha256"] = changedDigests,
				["restored_by_reimport"] = reimported,
				["restored_by_unload"] = unloaded,
				["not_restored"] = failed,
				["changed_on_disk"] = diskChanged,
			};
		}

		private static string Name(string path) => Path.GetFileNameWithoutExtension(path);

		private static string ProjectDir => Path.GetDirectoryName(Application.dataPath);

		private static string Sha256(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
				return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
		}
	}
}
