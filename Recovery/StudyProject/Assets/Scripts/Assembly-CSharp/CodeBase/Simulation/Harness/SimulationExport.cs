using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Codebase.Simulation.Systems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Harness
{
	// Job outputs isomorphic to the serialized SimulationData, plus the snapshot side files (design 1.4.3;
	// unit C1). simulation_data.json has exactly the two serialized fields of SimulationData, in field
	// declaration order: doubles as round-trip text, floats as G9, NaN/Infinity as bare symbols, enums as
	// integers. Every exported file is described in manifest "export": its sha256 and, for snapshot files,
	// a digest of the IEEE bits of every float and double in document order, which the Python ingest
	// recomputes from the parsed text (tufu_balance.unity.simio).
	public static class SimulationExport
	{
		public const string ExportSchema = "tufu.sim.export/1";
		public const string SimulationDataFile = "simulation_data.json";
		public const string TimeChunkFullFile = "timechunk_full.json";
		public const string SnapshotsExtFile = "snapshots_ext.csv";
		public const string ItemsExtFile = "items_ext.csv";
		public const string SlowTickBasisFile = "slow_tick_basis.csv";
		public const string ShippedDir = "shipped";

		public const string ListSnapshots = "Snapshots";
		public const string ListTimeChunks = "TimeChunkSnapshots";

		// Output switches (request job.outputs) and their defaults; an "empty" job exports only what it asks for.
		public static readonly Dictionary<string, bool> OutputDefaults = new Dictionary<string, bool>
		{
			["simulation_data"] = true,
			["timechunk_full"] = true,
			["snapshots_ext"] = true,
			["items_ext"] = true,
			["shipped_simulation_data"] = false,
		};

		// ---- per-snapshot state that SimulationSnapshot does not carry (filled by the engine, C3b) ----

		public struct ItemExt
		{
			public string Key;
			public int Level;
			public int Copies;
			public bool Evolved;
			public int EvolutionLevel;
		}

		public sealed class SnapshotExt
		{
			public int UnlockedWeaponSlots;
			public int UnlockedArmorSlots;
			public double CurrentHealth;
			public List<ItemExt> Items = new List<ItemExt>();
			public Dictionary<int, double> SlowTickBasis;
		}

		internal static Dictionary<int, double> SlowTickBasis;

		private static readonly Dictionary<string, SortedDictionary<int, SnapshotExt>> recorded = new Dictionary<string, SortedDictionary<int, SnapshotExt>>();

		// Called by MakeSnapshot right after it appends a snapshot: index is its position in
		// config.Snapshots (timeChunkList false) or config.TimeChunkSnapshots (true); items in Items order.
		public static void RecordSnapshot(bool timeChunkList, int index, SnapshotExt ext)
		{
			Record(recorded, timeChunkList ? ListTimeChunks : ListSnapshots, index, ext);
		}

		private static void Record(Dictionary<string, SortedDictionary<int, SnapshotExt>> record, string list, int index, SnapshotExt ext)
		{
			if (!record.TryGetValue(list, out var byIndex)) record[list] = byIndex = new SortedDictionary<int, SnapshotExt>();
			byIndex[index] = ext;
		}

		// Called when the engine starts a run with fresh snapshot lists; the harness also calls it at every job start.
		public static void ResetRecorder()
		{
			recorded.Clear();
			SlowTickBasis = null;
		}

		public static int RecordedCount => recorded.Values.Sum(x => x.Count);

		public static bool OutputEnabled(SimulationRunRequest.Job job, string name)
		{
			if (job.Outputs != null && job.Outputs.TryGetValue(name, out bool on)) return on;
			return job.Mode != "empty" && OutputDefaults.TryGetValue(name, out bool d) && d;
		}

		// ---- writers ----

		public sealed class ExportedFile
		{
			public string Sha256;
			public long Bytes;
			public int Doubles, Floats, NaNs;
			public string BitsSha256;
			public int Rows;

			public JObject ToJson()
			{
				var o = new JObject { ["sha256"] = Sha256, ["bytes"] = Bytes };
				if (BitsSha256 != null)
				{
					o["doubles"] = Doubles;
					o["floats"] = Floats;
					o["nans"] = NaNs;
					o["bits_sha256"] = BitsSha256;
				}
				else o["rows"] = Rows;
				return o;
			}
		}

		private sealed class Bits
		{
			public readonly StringBuilder Text = new StringBuilder();
			public int Doubles, Floats, NaNs;

			public void Double(double v)
			{
				Doubles++;
				if (double.IsNaN(v))
				{
					NaNs++;
					Text.Append("d:nan\n");
				}
				else Text.Append("d:").Append(BitConverter.DoubleToInt64Bits(v).ToString("x16", CultureInfo.InvariantCulture)).Append('\n');
			}

			public void Float(float v)
			{
				Floats++;
				if (float.IsNaN(v))
				{
					NaNs++;
					Text.Append("f:nan\n");
				}
				else Text.Append("f:").Append(BitConverter.SingleToInt32Bits(v).ToString("x8", CultureInfo.InvariantCulture)).Append('\n');
			}
		}

		// Text of a SimulationData-shaped object; lists given as null are omitted (timechunk_full has only
		// TimeChunkSnapshots).
		public static string SnapshotsJson(IList<SimulationSnapshot> snapshots, IList<SimulationSnapshot> timeChunks, out ExportedFile info)
		{
			var bits = new Bits();
			var text = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
			using (var w = new JsonTextWriter(text) { Formatting = Formatting.Indented, Indentation = 1, IndentChar = '\t' })
			{
				w.WriteStartObject();
				if (snapshots != null) WriteList(w, ListSnapshots, snapshots, bits);
				if (timeChunks != null) WriteList(w, ListTimeChunks, timeChunks, bits);
				w.WriteEndObject();
			}
			string json = text.ToString() + "\n";
			info = new ExportedFile
			{
				Doubles = bits.Doubles,
				Floats = bits.Floats,
				NaNs = bits.NaNs,
				BitsSha256 = SimulationHarness.Sha256(bits.Text.ToString()),
			};
			return json;
		}

		private static void WriteList(JsonTextWriter w, string name, IList<SimulationSnapshot> list, Bits bits)
		{
			w.WritePropertyName(name);
			w.WriteStartArray();
			foreach (SimulationSnapshot s in list)
			{
				w.WriteStartObject();
				w.WritePropertyName("Playtime");
				Float(w, s.Playtime, bits);
				w.WritePropertyName("TotalPlaytime");
				Float(w, s.TotalPlaytime, bits);
				w.WritePropertyName("Location");
				w.WriteValue(s.Location);
				w.WritePropertyName("TimeChunkIndex");
				w.WriteValue(s.TimeChunkIndex);
				w.WritePropertyName("Stats");
				w.WriteStartArray();
				foreach (SimulationStatData st in s.Stats ?? new List<SimulationStatData>())
				{
					w.WriteStartObject();
					w.WritePropertyName("Key");
					w.WriteValue((int)st.Key);
					Double(w, "Value", st.Value, bits);
					Double(w, "StatsFromFeaturesFlat", st.StatsFromFeaturesFlat, bits);
					Double(w, "PercentMultiplier", st.PercentMultiplier, bits);
					Double(w, "TotalFlatValue", st.TotalFlatValue, bits);
					Double(w, "TotalValue", st.TotalValue, bits);
					Double(w, "CurrentGpm", st.CurrentGpm, bits);
					w.WriteEndObject();
				}
				w.WriteEndArray();
				w.WritePropertyName("Items");
				w.WriteStartArray();
				foreach (SimulationItemData it in s.Items ?? new List<SimulationItemData>())
				{
					w.WriteStartObject();
					w.WritePropertyName("Key");
					w.WriteValue(it.Key ?? "");
					w.WritePropertyName("Level");
					w.WriteValue(it.Level);
					w.WritePropertyName("Copies");
					w.WriteValue(it.Copies);
					w.WriteEndObject();
				}
				w.WriteEndArray();
				WriteDps(w, "DpsData", s.DpsData, bits);
				WriteDps(w, "DefenseData", s.DefenseData, bits);
				w.WriteEndObject();
			}
			w.WriteEndArray();
		}

		private static void WriteDps(JsonTextWriter w, string name, List<SimulationDpsData> list, Bits bits)
		{
			w.WritePropertyName(name);
			w.WriteStartArray();
			foreach (SimulationDpsData d in list ?? new List<SimulationDpsData>())
			{
				w.WriteStartObject();
				w.WritePropertyName("WeaponType");
				w.WriteValue((int)d.WeaponType);
				Double(w, "Value", d.Value, bits);
				w.WriteEndObject();
			}
			w.WriteEndArray();
		}

		private static void Double(JsonTextWriter w, string name, double v, Bits bits)
		{
			w.WritePropertyName(name);
			w.WriteRawValue(SimulationJson.JsonDouble(v));
			bits.Double(v);
		}

		private static void Float(JsonTextWriter w, float v, Bits bits)
		{
			w.WriteRawValue(SimulationJson.JsonFloat(v));
			bits.Float(v);
		}

		public static ExportedFile WriteSnapshots(string path, IList<SimulationSnapshot> snapshots, IList<SimulationSnapshot> timeChunks)
		{
			string json = SnapshotsJson(snapshots, timeChunks, out ExportedFile info);
			return Finish(path, json, info);
		}

		private static ExportedFile Finish(string path, string text, ExportedFile info)
		{
			SimulationJson.WriteText(path, text);
			info.Sha256 = SimulationHarness.Sha256(text);
			info.Bytes = Encoding.UTF8.GetByteCount(text);
			return info;
		}

		// snapshots_ext.csv: one row per snapshot of the full lists; exported_index is the position in
		// simulation_data.json (TimeChunkSnapshots there is trimmed); the ext columns are empty when the
		// engine did not record the snapshot.
		public const string SnapshotsExtHeader = "list,index,exported_index,time_chunk_index,playtime,total_playtime,location,unlocked_weapon_slots,unlocked_armor_slots,current_health";

		public const string ItemsExtHeader = "list,index,item_index,key,level,copies,evolved,evolution_level";

		public static ExportedFile WriteSnapshotsExt(string path, IList<SimulationSnapshot> snapshots, IList<SimulationSnapshot> timeChunks, int[] timeChunkExported,
			Dictionary<string, SortedDictionary<int, SnapshotExt>> record = null)
		{
			record = record ?? recorded;
			var sb = new StringBuilder(SnapshotsExtHeader).Append('\n');
			int rows = 0;
			foreach (var (list, items, exported) in Lists(snapshots, timeChunks, timeChunkExported))
			{
				record.TryGetValue(list, out var ext);
				for (int i = 0; i < items.Count; i++)
				{
					SimulationSnapshot s = items[i];
					SnapshotExt e = null;
					ext?.TryGetValue(i, out e);
					sb.Append(list).Append(',').Append(i).Append(',').Append(exported == null ? i.ToString(CultureInfo.InvariantCulture) : exported[i] < 0 ? "" : exported[i].ToString(CultureInfo.InvariantCulture))
						.Append(',').Append(s.TimeChunkIndex).Append(',').Append(SimulationJson.FormatFloat(s.Playtime)).Append(',').Append(SimulationJson.FormatFloat(s.TotalPlaytime))
						.Append(',').Append(s.Location).Append(',')
						.Append(e == null ? "" : e.UnlockedWeaponSlots.ToString(CultureInfo.InvariantCulture)).Append(',')
						.Append(e == null ? "" : e.UnlockedArmorSlots.ToString(CultureInfo.InvariantCulture)).Append(',')
						.Append(e == null ? "" : SimulationJson.FormatDouble(e.CurrentHealth)).Append('\n');
					rows++;
				}
			}
			return Finish(path, sb.ToString(), new ExportedFile { Rows = rows });
		}

		// Exact GetForecastStatValue results at slow-tick cache time, before combat rewards.
		public static ExportedFile WriteSlowTickBasis(string path)
		{
			var sb = new StringBuilder("list,index,stat,value_hex\n");
			int rows = 0;
			if (recorded.TryGetValue(ListSnapshots, out var snapshots))
				foreach (var entry in snapshots)
					if (entry.Value.SlowTickBasis != null)
						foreach (var stat in entry.Value.SlowTickBasis.OrderBy(x => x.Key))
						{
							sb.Append(ListSnapshots).Append(',').Append(entry.Key).Append(',').Append(stat.Key).Append(',')
								.Append(BitConverter.DoubleToInt64Bits(stat.Value).ToString("x16", CultureInfo.InvariantCulture)).Append('\n');
							rows++;
						}
			return Finish(path, sb.ToString(), new ExportedFile { Rows = rows });
		}

		// items_ext.csv: one row per item of every snapshot (Key, Level, Copies from the snapshot); Evolved
		// and EvolutionLevel come from the engine's record and are empty without one.
		public static ExportedFile WriteItemsExt(string path, IList<SimulationSnapshot> snapshots, IList<SimulationSnapshot> timeChunks, List<string> problems,
			Dictionary<string, SortedDictionary<int, SnapshotExt>> record = null)
		{
			record = record ?? recorded;
			var sb = new StringBuilder(ItemsExtHeader).Append('\n');
			int rows = 0;
			foreach (var (list, items, _) in Lists(snapshots, timeChunks, null))
			{
				record.TryGetValue(list, out var ext);
				for (int i = 0; i < items.Count; i++)
				{
					List<SimulationItemData> inv = items[i].Items ?? new List<SimulationItemData>();
					SnapshotExt e = null;
					ext?.TryGetValue(i, out e);
					if (e != null && (e.Items.Count != inv.Count || e.Items.Where((x, k) => x.Key != inv[k].Key).Any()))
					{
						problems.Add(list + "[" + i + "]: recorded items do not match the snapshot's Items");
						e = null;
					}
					for (int k = 0; k < inv.Count; k++)
					{
						sb.Append(list).Append(',').Append(i).Append(',').Append(k).Append(',').Append(Csv(inv[k].Key)).Append(',')
							.Append(inv[k].Level).Append(',').Append(inv[k].Copies).Append(',')
							.Append(e == null ? "" : e.Items[k].Evolved ? "1" : "0").Append(',')
							.Append(e == null ? "" : e.Items[k].EvolutionLevel.ToString(CultureInfo.InvariantCulture)).Append('\n');
						rows++;
					}
				}
			}
			return Finish(path, sb.ToString(), new ExportedFile { Rows = rows });
		}

		private static IEnumerable<(string, IList<SimulationSnapshot>, int[])> Lists(IList<SimulationSnapshot> snapshots, IList<SimulationSnapshot> timeChunks, int[] timeChunkExported)
		{
			yield return (ListSnapshots, snapshots ?? new List<SimulationSnapshot>(), null);
			yield return (ListTimeChunks, timeChunks ?? new List<SimulationSnapshot>(), timeChunkExported);
		}

		// Position of each full-list snapshot in the trimmed list, -1 when trimmed away; null when the
		// trimmed list is not an ordered subsequence of the full one.
		public static int[] ExportedIndices(IList<SimulationSnapshot> full, IList<SimulationSnapshot> trimmed)
		{
			var result = new int[full.Count];
			int j = 0;
			for (int i = 0; i < full.Count; i++)
			{
				if (j < trimmed.Count && full[i].TimeChunkIndex == trimmed[j].TimeChunkIndex &&
				    BitConverter.SingleToInt32Bits(full[i].Playtime) == BitConverter.SingleToInt32Bits(trimmed[j].Playtime))
					result[i] = j++;
				else result[i] = -1;
			}
			return j == trimmed.Count ? result : null;
		}

		// ---- shipped probe: the side-file writers on real lists before the engine records anything ----
		// Rules mirrored by tests/unity/test_export_roundtrip.py::test_shipped_side_files_follow_the_probe.

		public const string ProbeTrimRule = "TimeChunkSnapshots[i] kept when i % 3 != 1";
		public const string ProbeRecordRule = "Snapshots[i] when i % 4 != 3, TimeChunkSnapshots[i] when i % 5 != 2: "
			+ "weapon slots i % 6, armor slots Location, health TotalPlaytime / 3.0, item k evolved (i + k) % 2 == 0, "
			+ "evolution level Level * 10 + Copies; Snapshots[1] recorded without its last item (a mismatch)";

		public static JObject WriteShippedProbe(string dir, SimulationData shipped, JObject files)
		{
			List<SimulationSnapshot> snapshots = shipped.Snapshots ?? new List<SimulationSnapshot>();
			List<SimulationSnapshot> full = shipped.TimeChunkSnapshots ?? new List<SimulationSnapshot>();
			List<SimulationSnapshot> trimmed = full.Where((x, i) => i % 3 != 1).ToList();
			int[] exported = ExportedIndices(full, trimmed);
			var reversed = Enumerable.Reverse(trimmed).ToList();
			bool notSubsequenceNull = trimmed.Count < 2 || ExportedIndices(full, reversed) == null;

			var record = new Dictionary<string, SortedDictionary<int, SnapshotExt>>();
			int recordedCount = 0;
			foreach (var (list, items, period, gap) in new[] { (ListSnapshots, snapshots, 4, 3), (ListTimeChunks, full, 5, 2) })
			{
				for (int i = 0; i < items.Count; i++)
				{
					if (i % period == gap) continue;
					SimulationSnapshot s = items[i];
					var ext = new SnapshotExt { UnlockedWeaponSlots = i % 6, UnlockedArmorSlots = s.Location, CurrentHealth = s.TotalPlaytime / 3.0 };
					List<SimulationItemData> inv = s.Items ?? new List<SimulationItemData>();
					int count = list == ListSnapshots && i == 1 ? inv.Count - 1 : inv.Count;
					for (int k = 0; k < count; k++)
						ext.Items.Add(new ItemExt { Key = inv[k].Key, Level = inv[k].Level, Copies = inv[k].Copies, Evolved = (i + k) % 2 == 0, EvolutionLevel = inv[k].Level * 10 + inv[k].Copies });
					Record(record, list, i, ext);
					recordedCount++;
				}
			}
			var problems = new List<string>();
			files[ShippedDir + "/" + SnapshotsExtFile] = WriteSnapshotsExt(Path.Combine(dir, SnapshotsExtFile), snapshots, full, exported, record).ToJson();
			files[ShippedDir + "/" + ItemsExtFile] = WriteItemsExt(Path.Combine(dir, ItemsExtFile), snapshots, full, problems, record).ToJson();
			return new JObject
			{
				["trim_rule"] = ProbeTrimRule,
				["record_rule"] = ProbeRecordRule,
				["trimmed"] = trimmed.Count,
				["recorded"] = recordedCount,
				["not_subsequence_is_null"] = notSubsequenceNull,
				["problems"] = new JArray(problems.Cast<object>().ToArray()),
			};
		}

		private static string Csv(string s) => s == null ? "" : s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
	}

	public sealed partial class SimulationHarness
	{
		// C1's export hook (design 1.4.3): runs after the stop, before manifest.json is written.
		partial void RebuildJobExportOutputs(SimulationRunRequest.Job current, string jobDir, JObject jobManifest)
		{
			var files = new JObject();
			var export = new JObject { ["schema"] = SimulationExport.ExportSchema, ["files"] = files };
			var problems = new List<string>();
			bool any = false;
			SimulationConfig cfg = config;
			List<SimulationSnapshot> snapshots = cfg.Snapshots ?? new List<SimulationSnapshot>();
			List<SimulationSnapshot> full = cfg.TimeChunkSnapshots ?? new List<SimulationSnapshot>();

			if (SimulationExport.OutputEnabled(current, "simulation_data"))
			{
				any = true;
				List<SimulationSnapshot> trimmed = TrimmedTimeChunks(cfg, export);
				int[] exported = SimulationExport.ExportedIndices(full, trimmed);
				if (exported == null) problems.Add("trimmed TimeChunkSnapshots are not an ordered subsequence of the full list");
				files[SimulationExport.SimulationDataFile] = SimulationExport.WriteSnapshots(Path.Combine(jobDir, SimulationExport.SimulationDataFile), snapshots, trimmed).ToJson();
				if (SimulationExport.OutputEnabled(current, "snapshots_ext"))
				{
					files[SimulationExport.SnapshotsExtFile] = SimulationExport.WriteSnapshotsExt(Path.Combine(jobDir, SimulationExport.SnapshotsExtFile), snapshots, full, exported).ToJson();
					files[SimulationExport.SlowTickBasisFile] = SimulationExport.WriteSlowTickBasis(Path.Combine(jobDir, SimulationExport.SlowTickBasisFile)).ToJson();
				}
			}
			if (SimulationExport.OutputEnabled(current, "timechunk_full"))
			{
				any = true;
				files[SimulationExport.TimeChunkFullFile] = SimulationExport.WriteSnapshots(Path.Combine(jobDir, SimulationExport.TimeChunkFullFile), null, full).ToJson();
			}
			if (SimulationExport.OutputEnabled(current, "items_ext"))
			{
				any = true;
				files[SimulationExport.ItemsExtFile] = SimulationExport.WriteItemsExt(Path.Combine(jobDir, SimulationExport.ItemsExtFile), snapshots, full, problems).ToJson();
			}
			if (SimulationExport.OutputEnabled(current, "shipped_simulation_data"))
			{
				// The shipped asset through the same exporter, twice: the round-trip reference (design 7.3 C1).
				any = true;
				SimulationData shipped = SimulationSystem.Instance.SimulationData;
				string first = SimulationExport.SnapshotsJson(shipped.Snapshots, shipped.TimeChunkSnapshots, out _);
				string rel = SimulationExport.ShippedDir + "/" + SimulationExport.SimulationDataFile;
				SimulationExport.ExportedFile info = SimulationExport.WriteSnapshots(Path.Combine(jobDir, SimulationExport.ShippedDir, SimulationExport.SimulationDataFile), shipped.Snapshots, shipped.TimeChunkSnapshots);
				files[rel] = info.ToJson();
				export["shipped_twice_identical"] = first == File.ReadAllText(Path.Combine(jobDir, SimulationExport.ShippedDir, SimulationExport.SimulationDataFile), new UTF8Encoding(false));
				export["shipped_asset"] = shipped.name;
				export["shipped_probe"] = SimulationExport.WriteShippedProbe(Path.Combine(jobDir, SimulationExport.ShippedDir), shipped, files);
			}
			if (!any) return;
			export["snapshots"] = snapshots.Count;
			export["time_chunk_snapshots_full"] = full.Count;
			export["trim_simulation_data"] = cfg.TrimSimulationData;
			export["recorded_snapshot_ext"] = SimulationExport.RecordedCount;
			export["problems"] = new JArray(problems.Cast<object>().ToArray());
			jobManifest["export"] = export;
			if (problems.Count > 0) UnityEngine.Debug.LogWarning(LogTag + "export: " + string.Join("; ", problems.Take(10)));
		}

		// GetTrimmedTimeChunkSnapshots is the original trimming rule (filled by C3b); until it returns a list
		// the export is untrimmed and says so.
		private static List<SimulationSnapshot> TrimmedTimeChunks(SimulationConfig cfg, JObject export)
		{
			MethodInfo trim = typeof(SimulationConfig).GetMethod("GetTrimmedTimeChunkSnapshots", BindingFlags.Instance | BindingFlags.NonPublic);
			var trimmed = trim?.Invoke(cfg, null) as List<SimulationSnapshot>;
			export["time_chunk_trim"] = trimmed != null ? "GetTrimmedTimeChunkSnapshots" : "none (GetTrimmedTimeChunkSnapshots returned null)";
			return trimmed ?? cfg.TimeChunkSnapshots ?? new List<SimulationSnapshot>();
		}
	}
}
