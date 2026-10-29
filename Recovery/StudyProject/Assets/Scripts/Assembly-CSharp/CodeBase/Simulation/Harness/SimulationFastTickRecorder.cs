using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Harness
{
	// Fast-phase outputs of a batch job (unit C5); the engine (SimulationSystem.FastTick.cs) fills it, interactive
	// runs have none. Both files are written while the job runs, one day at a time:
	//   fasttick_days.jsonl  (output fasttick_days, on by default) one line per FastTick: planning rounds, clears,
	//                        active minutes, the accumulated rewards the day's flush applied (doubles as hex bits,
	//                        SimulationRuntimeProbes.D/F), unlocks by phase
	//   spawner_days.csv.gz  (output spawner_days, off by default; design 1.4.3) one row per spawner of each planning
	//                        round's bot data (CalculateSpawnerData(false)): the kill gate's inputs and the fights;
	//                        each day is its own gzip member (Python's gzip reads the concatenation)
	internal sealed class SimulationFastTickRecorder
	{
		public const string Schema = "tufu.sim.fast_tick/1";
		public const string DaysFile = "fasttick_days.jsonl";
		public const string DaysOutput = "fasttick_days";
		public const string SpawnerDaysFile = "spawner_days.csv.gz";
		public const string SpawnerDaysOutput = "spawner_days";
		public const string SpawnerDaysHeader = "day,round,spawner,location,dafc,killable,fights,fight_time,farmed_time,loss,max_health";

		private static SimulationFastTickRecorder current;

		private SimulationRunRequest.Job job;

		private string daysPath;

		private string spawnerDaysPath;

		private bool headerWritten;

		private bool started;

		private readonly StringBuilder rows = new StringBuilder();

		internal readonly SortedDictionary<string, long> Counters = new SortedDictionary<string, long>(StringComparer.Ordinal);

		internal string AbVariant { get; private set; }

		internal bool ReferenceFlush { get; private set; }

		internal bool SpawnerDays => spawnerDaysPath != null;

		internal string Error;

		// Islands the day's flush opened, and its stat modifier calls (FlushFastTickChunkRewards).
		internal readonly List<int> FlushUnlocks = new List<int>();

		internal int FlushStatModifiers;

		// The recorder of the current batch job (made on first use), null when no job runs.
		internal static SimulationFastTickRecorder For(SimulationRunRequest.Job job)
		{
			if (job == null) return null;
			if (current != null && ReferenceEquals(current.job, job)) return current;
			current = new SimulationFastTickRecorder { job = job, AbVariant = SimulationSandbox.Request?.Sandbox?.AbVariant ?? "A" };
#if UNITY_EDITOR
			current.ReferenceFlush = job.FastTick != null && job.FastTick.ReferenceFlush;
#endif
			string dir = SimulationSandbox.Request?.JobDir(job);
			if (dir != null && Enabled(job, DaysOutput, true)) current.daysPath = Path.Combine(dir, DaysFile);
			if (dir != null && Enabled(job, SpawnerDaysOutput, false)) current.spawnerDaysPath = Path.Combine(dir, SpawnerDaysFile);
			return current;
		}

		private static bool Enabled(SimulationRunRequest.Job job, string name, bool byDefault) =>
			job.Outputs != null && job.Outputs.TryGetValue(name, out bool on) ? on : byDefault;

		// Files appear with the first fast day (a job that never reaches day 4 has none).
		private void Start()
		{
			if (started) return;
			started = true;
			foreach (string path in new[] { daysPath, spawnerDaysPath })
			{
				if (path == null) continue;
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				if (File.Exists(path)) File.Delete(path);
			}
		}

		internal void BeginFlush()
		{
			FlushUnlocks.Clear();
			FlushStatModifiers = 0;
		}

		internal void Count(string key, long n = 1)
		{
			Counters.TryGetValue(key, out long value);
			Counters[key] = value + n;
		}

		internal void AddSpawnerRow(int day, int round, string spawner, int location, bool dafc, bool killable, int fights, double fightTime,
			double farmedTime, double loss, double maxHealth)
		{
			if (spawnerDaysPath == null) return;
			rows.Append(day).Append(',').Append(round).Append(',').Append(Csv(spawner)).Append(',').Append(location).Append(',')
				.Append(dafc ? '1' : '0').Append(',').Append(killable ? '1' : '0').Append(',').Append(fights).Append(',')
				.Append(SimulationJson.FormatDouble(fightTime)).Append(',').Append(SimulationJson.FormatDouble(farmedTime)).Append(',')
				.Append(SimulationJson.FormatDouble(loss)).Append(',').Append(SimulationJson.FormatDouble(maxHealth)).Append('\n');
		}

		private static string Csv(string text) =>
			text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\"";

		// The day's line and its spawner rows (a gzip member of its own).
		internal void WriteDay(JObject record)
		{
			try
			{
				Start();
				if (daysPath != null) File.AppendAllText(daysPath, record.ToString(Formatting.None) + "\n", new UTF8Encoding(false));
				if (spawnerDaysPath == null) return;
				string text = (headerWritten ? "" : SpawnerDaysHeader + "\n") + rows;
				rows.Clear();
				headerWritten = true;
				using (var file = new FileStream(spawnerDaysPath, FileMode.Append, FileAccess.Write))
				using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
				{
					byte[] bytes = new UTF8Encoding(false).GetBytes(text);
					gzip.Write(bytes, 0, bytes.Length);
				}
			}
			catch (Exception e)
			{
				if (Error == null) Error = "fast tick outputs: " + e.GetType().Name + ": " + e.Message;
			}
		}

		internal JObject Files() => new JObject
		{
			[DaysOutput] = started && daysPath != null ? DaysFile : null,
			[SpawnerDaysOutput] = started && spawnerDaysPath != null ? SpawnerDaysFile : null,
		};

		internal JObject CountersJson() => new JObject(Counters.Select(kv => new JProperty(kv.Key, kv.Value)));
	}
}
