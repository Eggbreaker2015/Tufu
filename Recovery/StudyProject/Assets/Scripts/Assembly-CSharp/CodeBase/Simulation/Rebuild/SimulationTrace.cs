using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Codebase.Simulation.Systems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Rebuild
{
	// Daily records, events and state digests of a run (design 1.4.3, 2.6; unit C3c). The engine fills
	// SimulationTrace.Current (SimulationSystem.Trace.cs); SimulationHarness writes it when the job ends
	// (formats: tufu_balance.unity.schema and tufu_balance.unity.trace):
	//   daily.csv     one row per day: complete days at their day change, the open day (complete = 0) at the end
	//   events.jsonl  first_kill, unlock, slot_unlock, evolve, death, in the order they happen
	//   digests.csv   per day, right after the day's forced snapshot and override: sha256 of the state text
	//   digests.txt   the state texts themselves (output "digest_text", off by default)
	public sealed class SimulationTrace
	{
		public const string Schema = "tufu.sim.trace/1";
		public const string DailyFile = "daily.csv", EventsFile = "events.jsonl", DigestsFile = "digests.csv", DigestTextFile = "digests.txt";

		public const string DailyHeader = "day,time_chunk_index,playtime,total_playtime,active_minutes,offline_minutes,kills,clears,"
			+ "fight_time_sum,farmed_time_sum,eligible_count,eligible_signature,location,unlocked_location,ticks,offline_steps,complete";

		public const string DigestsHeader = "day,time_chunk_index,playtime,total_playtime,sha256";

		// Output switches (request job.outputs) and their defaults for non-empty jobs.
		public static readonly Dictionary<string, bool> OutputDefaults = new Dictionary<string, bool>
		{
			["daily"] = true,
			["events"] = true,
			["digests"] = true,
			["digest_text"] = false,
		};

		public static SimulationTrace Current { get; private set; }

		// A new recorder for a run that starts: its job (null when interactive), parameter report (manifest
		// "params") and random source.
		public static SimulationTrace Begin(object job, JObject paramsReport, SimulationRandom random, bool keepDigestText)
		{
			Current = new SimulationTrace { Job = job, Params = paramsReport, Random = random, KeepDigestText = keepDigestText };
			return Current;
		}

		public object Job { get; private set; }

		public JObject Params { get; private set; }

		public SimulationRandom Random { get; private set; }

		public bool KeepDigestText { get; private set; }

		// State of the open day, kept by the engine.
		internal int DayIndex = -1;
		internal int DaySnapshotIndex = -1;
		internal float DayStartPlaytime;
		internal long DayStartKills, DayStartClears;
		internal double FightTimeSum, FarmedTimeSum;
		internal int EligibleCount = -1;
		internal string EligibleSignature = "";
		internal readonly HashSet<string> Killed = new HashSet<string>(StringComparer.Ordinal);
		// Charges used by the day that closed last ("hierarchy path|Key" -> usage), taken before the day change
		// clears them; the next day's digest carries them as charges_closed.*.
		internal readonly SortedDictionary<string, int> ClosedCharges = new SortedDictionary<string, int>(StringComparer.Ordinal);
		internal int ClosedChargesDay = -1;
		// Spawner identity ("hierarchy path|Key") cache.
		internal readonly Dictionary<object, string> Ids = new Dictionary<object, string>();

		private readonly StringBuilder daily = new StringBuilder(DailyHeader + "\n");
		private readonly StringBuilder events = new StringBuilder();
		private readonly StringBuilder digests = new StringBuilder(DigestsHeader + "\n");
		private readonly StringBuilder digestTexts = new StringBuilder();
		private readonly SortedDictionary<string, int> eventCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
		private int dailyRows, completeDays, digestRows;
		private string lastDigest;

		public struct Day
		{
			public int DayIndex, SnapshotIndex;
			public float Playtime, TotalPlaytime;
			public double ActiveMinutes, OfflineMinutes, FightTimeSum, FarmedTimeSum;
			public long Kills, Clears;
			public int EligibleCount;
			public string EligibleSignature;
			public int Location, UnlockedLocation, Ticks, OfflineSteps;
			public bool Complete;
		}

		internal void AddDay(Day d)
		{
			daily.Append(d.DayIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(d.SnapshotIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(F(d.Playtime)).Append(',').Append(F(d.TotalPlaytime)).Append(',')
				.Append(D(d.ActiveMinutes)).Append(',').Append(D(d.OfflineMinutes)).Append(',')
				.Append(d.Kills.ToString(CultureInfo.InvariantCulture)).Append(',').Append(d.Clears.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(D(d.FightTimeSum)).Append(',').Append(D(d.FarmedTimeSum)).Append(',')
				.Append(d.EligibleCount.ToString(CultureInfo.InvariantCulture)).Append(',').Append(d.EligibleSignature ?? "").Append(',')
				.Append(d.Location.ToString(CultureInfo.InvariantCulture)).Append(',').Append(d.UnlockedLocation.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(d.Ticks.ToString(CultureInfo.InvariantCulture)).Append(',').Append(d.OfflineSteps.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(d.Complete ? '1' : '0').Append('\n');
			dailyRows++;
			if (d.Complete) completeDays++;
		}

		// One event: the common fields, then `fields` in order (string, int, long, float, double or bool values).
		internal void AddEvent(string type, int day, float playtime, float totalPlaytime, params (string Name, object Value)[] fields)
		{
			var sb = new StringBuilder("{\"type\":").Append(JsonConvert.ToString(type))
				.Append(",\"day\":").Append(day.ToString(CultureInfo.InvariantCulture))
				.Append(",\"playtime\":").Append(SimulationHarnessJson.Float(playtime))
				.Append(",\"total_playtime\":").Append(SimulationHarnessJson.Float(totalPlaytime));
			foreach (var (name, value) in fields)
			{
				sb.Append(',').Append(JsonConvert.ToString(name)).Append(':');
				switch (value)
				{
					case null: sb.Append("null"); break;
					case string s: sb.Append(JsonConvert.ToString(s)); break;
					case bool b: sb.Append(b ? "true" : "false"); break;
					case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
					case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
					case float f: sb.Append(SimulationHarnessJson.Float(f)); break;
					case double x: sb.Append(SimulationHarnessJson.Double(x)); break;
					default: throw new ArgumentException("event field " + name + ": unsupported value " + value.GetType().Name);
				}
			}
			events.Append(sb).Append("}\n");
			eventCounts.TryGetValue(type, out int n);
			eventCounts[type] = n + 1;
		}

		internal void AddDigest(int day, int snapshotIndex, float playtime, float totalPlaytime, string text)
		{
			string sha = Sha256(text);
			digests.Append(day.ToString(CultureInfo.InvariantCulture)).Append(',').Append(snapshotIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(F(playtime)).Append(',').Append(F(totalPlaytime)).Append(',').Append(sha).Append('\n');
			digestRows++;
			lastDigest = sha;
			if (KeepDigestText)
				digestTexts.Append("# day ").Append(day.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(sha).Append('\n').Append(text);
		}

		public int DailyRows => dailyRows;

		public string LastDigest => lastDigest;

		// Writes the switched-on files into `dir`; returns manifest "trace".
		public JObject Write(string dir, Func<string, bool> enabled)
		{
			var files = new JObject();
			if (enabled("daily")) files[DailyFile] = WriteFile(dir, DailyFile, daily.ToString(), dailyRows);
			if (enabled("events")) files[EventsFile] = WriteFile(dir, EventsFile, events.ToString(), eventCounts.Values.Sum());
			if (enabled("digests")) files[DigestsFile] = WriteFile(dir, DigestsFile, digests.ToString(), digestRows);
			if (enabled("digest_text") && KeepDigestText) files[DigestTextFile] = WriteFile(dir, DigestTextFile, digestTexts.ToString(), digestRows);
			var trace = new JObject
			{
				["schema"] = Schema,
				["files"] = files,
				["days"] = dailyRows,
				["complete_days"] = completeDays,
				["digests"] = digestRows,
				["last_digest"] = lastDigest,
				["events"] = new JObject(eventCounts.Select(kv => new JProperty(kv.Key, kv.Value))),
			};
			if (Random != null)
			{
				trace["random"] = new JObject
				{
					["mode"] = (int)Random.Mode,
					["seed"] = Random.Seed,
					["streams"] = Random.StreamCount,
					["state_sha256"] = Sha256(Random.StateText()),
				};
			}
			return trace;
		}

		private static JObject WriteFile(string dir, string name, string text, int rows)
		{
			string path = Path.Combine(dir, name);
			string tmp = path + ".tmp";
			Directory.CreateDirectory(dir);
			File.WriteAllText(tmp, text, new UTF8Encoding(false));
			if (File.Exists(path)) File.Delete(path);
			File.Move(tmp, path);
			return new JObject { ["sha256"] = Sha256(text), ["bytes"] = new UTF8Encoding(false).GetByteCount(text), ["rows"] = rows };
		}

		internal static string Sha256(string text) => SimulationCanonicalJson.Sha256(text);

		private static string F(float v) => Harness.SimulationJson.FormatFloat(v);

		private static string D(double v) => Harness.SimulationJson.FormatDouble(v);

		// Spawner set signature: sha256 of the sorted identities, one per line; "" for none.
		public static string SetSignature(IEnumerable<string> ids)
		{
			var sorted = ids.OrderBy(x => x, StringComparer.Ordinal).ToList();
			return sorted.Count == 0 ? "" : Sha256(string.Join("\n", sorted) + "\n");
		}
	}

	internal static class SimulationHarnessJson
	{
		public static string Float(float v) => Harness.SimulationJson.JsonFloat(v);

		public static string Double(double v) => Harness.SimulationJson.JsonDouble(v);
	}
}

namespace Codebase.Simulation.Harness
{
	using Codebase.Simulation.Rebuild;

	public sealed partial class SimulationHarness
	{
		// C3c's export hook (design 1.4.3, 2.6): after C1's outputs, before manifest.json is written. A failed job
		// (jobDir null, from Complete) gets the parameter report only.
		partial void RebuildJobExportTrace(SimulationRunRequest.Job current, string jobDir, JObject jobManifest)
		{
			if (current == null || current.Mode == "empty") return;
			SimulationTrace trace = SimulationTrace.Current;
			if (trace == null || !ReferenceEquals(trace.Job, current)) return;
			if (trace.Params != null) jobManifest["params"] = trace.Params;
			if (jobDir == null) return;
			SimulationSystem engine = SimulationSystem.Instance;
			engine?.RebuildTraceFlush();
			bool Enabled(string name) =>
				current.Outputs != null && current.Outputs.TryGetValue(name, out bool on) ? on : SimulationTrace.OutputDefaults.TryGetValue(name, out bool d) && d;
			jobManifest["trace"] = trace.Write(jobDir, Enabled);
		}
	}
}
