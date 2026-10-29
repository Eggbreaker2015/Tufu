using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Harness
{
	// Replay and checkpoint state of a job (design 4.6; unit C4). The harness hook RebuildJobApplyOverride starts it
	// before the run; the engine (SimulationSystem.Replay.cs) asks it for the state of each day right after the
	// day's forced snapshot and writes what it set back here:
	//   replay      every day d >= from_day: SimulationData.GetByTimeChunkIndex(d) of the shipped asset (recovered
	//               code): stat Values, the items of the snapshot side that provides them, stored DpsData/DefenseData
	//   checkpoint  every shipped day snapshot d >= from_day: that snapshot itself; closed loop in between
	// Output: manifest "override" (options, counters) and override_days.jsonl, one line per applied day with the
	// state the engine holds afterwards (doubles and floats as hex bits, SimulationRuntimeProbes.D/F).
	public sealed class SimulationStateOverride
	{
		public const string Schema = "tufu.sim.override/1";
		public const string DaysFile = "override_days.jsonl";
		public const string OutputName = "override_days";

		public sealed class ItemSpec
		{
			public string Key;
			public int Level;
			public int Copies;
			public bool Evolved;
		}

		// What the engine sets on one day. Snapshot is the interpolated (replay) or shipped (checkpoint) snapshot.
		public sealed class DayState
		{
			public int Day;
			public SimulationSnapshot Snapshot;
			public int ItemsIndex;
			public int ItemsTimeChunk;
			public bool SetItems;
			public List<ItemSpec> Items;
			public SimulationRunRequest.SlotCounts Slots;
			public bool StoredCaches;
			public int? UnlockTo;
			public float T = float.NaN;
			public int LeftTimeChunk = -1, RightTimeChunk = -1;
		}

		public static SimulationStateOverride Current { get; private set; }

		public SimulationRunRequest.Job Job { get; private set; }
		public string Mode { get; private set; }
		public int FromDay { get; private set; }
		public bool StoredCaches { get; private set; }
		public bool ItemsEveryDay { get; private set; }
		public bool UnlockFromSnapshot { get; private set; }
		// Set when the job cannot run its override (the engine fails the job with exit code 5 on day 0).
		public string Error { get; private set; }

		private SimulationRunRequest.OverrideOptions options;
		private SimulationData shipped;
		private JObject summary;
		private string daysPath;
		private int lastDay = -1;
		private bool itemsSet;
		private int cachesDay = -1;
		private List<SimulationDpsData> cachedDps, cachedDefense;
		private readonly SortedDictionary<string, long> counters = new SortedDictionary<string, long>(StringComparer.Ordinal);

		// The harness hook: replay/checkpoint jobs get a state, other jobs none (a given override is recorded as
		// inactive).
		public static void Begin(SimulationRunRequest.Job job, JObject manifest, string jobDir, SimulationConfig config)
		{
			Current = null;
			if (job == null) return;
			if (!SimulationRunRequest.OverrideModes.Contains(job.Mode))
			{
				if (job.Override != null) manifest["override"] = new JObject { ["schema"] = Schema, ["mode"] = job.Mode, ["active"] = false };
				return;
			}
			SimulationRunRequest.OverrideOptions o = job.Override ?? new SimulationRunRequest.OverrideOptions();
			var state = new SimulationStateOverride
			{
				Job = job,
				Mode = job.Mode,
				FromDay = o.FromDay ?? 0,
				StoredCaches = o.EconCaches == "stored",
				ItemsEveryDay = o.ReplayItems == "snapshot",
				UnlockFromSnapshot = o.Unlock == "snapshot",
				options = o,
				shipped = config != null ? config.SimulationData : null,
			};
			if (state.shipped == null || state.shipped.TimeChunkSnapshots == null || state.shipped.TimeChunkSnapshots.Count == 0)
				state.Error = "override: the shipped SimulationData has no TimeChunkSnapshots";
			bool record = job.Outputs == null || !job.Outputs.TryGetValue(OutputName, out bool on) || on;
			if (record && jobDir != null)
			{
				Directory.CreateDirectory(jobDir);
				state.daysPath = Path.Combine(jobDir, DaysFile);
				if (File.Exists(state.daysPath)) File.Delete(state.daysPath);
			}
			state.summary = new JObject
			{
				["schema"] = Schema,
				["mode"] = job.Mode,
				["active"] = true,
				["from_day"] = state.FromDay,
				["econ_caches"] = o.EconCaches,
				["replay_items"] = o.ReplayItems,
				["unlock"] = o.Unlock,
				["items_evolved"] = o.ItemsEvolved != null ? "request" : "level_ne_copies",
				["slots"] = o.Slots != null ? "request" : "unchanged",
				["shipped_asset"] = state.shipped != null ? state.shipped.name : null,
				["shipped_time_chunks"] = state.shipped?.TimeChunkSnapshots?.Count ?? 0,
				["file"] = state.daysPath != null ? DaysFile : null,
				["calls"] = 0,
				["applied"] = 0,
				["first_day"] = null,
				["last_day"] = null,
				["error"] = state.Error,
				["counters"] = new JObject(),
			};
			manifest["override"] = state.summary;
			Current = state;
		}

		// The state of the running batch job, or null (interactive runs, free/empty jobs, a stale earlier job).
		public static SimulationStateOverride For(SimulationRunRequest.Job job)
		{
			SimulationStateOverride current = Current;
			return current != null && job != null && ReferenceEquals(current.Job, job) ? current : null;
		}

		public void Count(string key, long n = 1)
		{
			counters.TryGetValue(key, out long value);
			counters[key] = value + n;
			summary["counters"] = new JObject(counters.Select(kv => new JProperty(kv.Key, kv.Value)));
		}

		// Null when nothing is overridden on `day`: before from_day, a checkpoint day without a shipped snapshot, or a
		// second call for the same day.
		public DayState Resolve(int day)
		{
			summary["calls"] = (int)summary["calls"] + 1;
			if (day < FromDay) return null;
			if (day == lastDay)
			{
				Count("repeat_day_calls");
				return null;
			}
			List<SimulationSnapshot> list = shipped.TimeChunkSnapshots;
			var state = new DayState { Day = day };
			if (Mode == "replay")
			{
				state.Snapshot = shipped.GetByTimeChunkIndex(day);
				state.ItemsIndex = IndexOfItems(list, state.Snapshot.Items);
				Bracket(list, day, state);
			}
			else
			{
				state.ItemsIndex = list.FindIndex(s => s.TimeChunkIndex == day);
				if (state.ItemsIndex < 0) return null;
				state.Snapshot = list[state.ItemsIndex];
			}
			if (state.ItemsIndex < 0)
			{
				Count("items_source_not_found");
				state.ItemsIndex = 0;
			}
			state.ItemsTimeChunk = list[state.ItemsIndex].TimeChunkIndex;
			string key = state.ItemsTimeChunk.ToString(CultureInfo.InvariantCulture);
			state.SetItems = ItemsEveryDay || !itemsSet;
			if (state.SetItems) state.Items = ItemSpecs(state.Snapshot.Items, key);
			if (options.Slots != null)
			{
				if (options.Slots.TryGetValue(key, out SimulationRunRequest.SlotCounts slots)) state.Slots = slots;
				else Count("slots_missing");
			}
			state.StoredCaches = StoredCaches;
			if (UnlockFromSnapshot) state.UnlockTo = state.Snapshot.Location;
			return state;
		}

		// The engine has set `state` (before it recomputes the caches, which read the stored ones of this day).
		public void Applied(DayState state)
		{
			lastDay = state.Day;
			if (state.SetItems) itemsSet = true;
			if (state.StoredCaches)
			{
				cachesDay = state.Day;
				cachedDps = Copy(state.Snapshot.DpsData);
				cachedDefense = Copy(state.Snapshot.DefenseData);
			}
			int applied = (int)summary["applied"] + 1;
			summary["applied"] = applied;
			if (applied == 1) summary["first_day"] = state.Day;
			summary["last_day"] = state.Day;
		}

		// The day's line in override_days.jsonl: the state the engine holds after the override.
		public void Record(JObject record)
		{
			if (daysPath != null) File.AppendAllText(daysPath, record.ToString(Formatting.None) + "\n", new UTF8Encoding(false));
		}

		// Stored caches hold for the day they were set on (replay: every day; checkpoint: the checkpoint day only).
		public bool TryStoredCaches(int day, out List<SimulationDpsData> dps, out List<SimulationDpsData> defense)
		{
			bool hit = cachesDay >= 0 && cachesDay == day && cachedDps != null;
			dps = hit ? cachedDps : null;
			defense = hit ? cachedDefense : null;
			return hit;
		}

		private List<ItemSpec> ItemSpecs(List<SimulationItemData> items, string timeChunkKey)
		{
			Dictionary<string, bool?> evolved = null;
			if (options.ItemsEvolved != null && !options.ItemsEvolved.TryGetValue(timeChunkKey, out evolved)) Count("items_evolved_missing_day");
			var specs = new List<ItemSpec>();
			foreach (SimulationItemData item in items ?? new List<SimulationItemData>())
			{
				bool? flag = null;
				if (evolved != null && evolved.TryGetValue(item.Key, out bool? given)) flag = given;
				else if (options.ItemsEvolved != null && evolved != null) Count("items_evolved_missing_item");
				if (flag == null)
				{
					if (options.ItemsEvolved != null && evolved != null && evolved.ContainsKey(item.Key)) Count("items_evolved_undetermined");
					flag = item.Level != item.Copies;
					Count("items_evolved_fallback");
				}
				if (!flag.Value && item.Level != item.Copies) Count("unevolved_level_ne_copies");
				specs.Add(new ItemSpec { Key = item.Key, Level = item.Level, Copies = item.Copies, Evolved = flag.Value });
			}
			return specs;
		}

		private int IndexOfItems(List<SimulationSnapshot> list, List<SimulationItemData> items)
		{
			int found = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (!ReferenceEquals(list[i].Items, items)) continue;
				if (found >= 0)
				{
					Count("items_source_ambiguous");
					break;
				}
				found = i;
			}
			return found;
		}

		// The bracket GetByTimeChunkIndex interpolates in, for the record only (it does not change the state).
		private static void Bracket(List<SimulationSnapshot> list, int day, DayState state)
		{
			var ordered = list.OrderBy(s => s.TimeChunkIndex).ToList();
			float x = day;
			if (x <= ordered[0].TimeChunkIndex || !(x < ordered[ordered.Count - 1].TimeChunkIndex)) return;
			for (int i = 1; i < ordered.Count; i++)
			{
				if (x < ordered[i - 1].TimeChunkIndex || x > ordered[i].TimeChunkIndex) continue;
				state.LeftTimeChunk = ordered[i - 1].TimeChunkIndex;
				state.RightTimeChunk = ordered[i].TimeChunkIndex;
				state.T = (x - state.LeftTimeChunk) / (state.RightTimeChunk - state.LeftTimeChunk);
				return;
			}
		}

		private static List<SimulationDpsData> Copy(List<SimulationDpsData> data) =>
			(data ?? new List<SimulationDpsData>()).Select(x => new SimulationDpsData { WeaponType = x.WeaponType, Value = x.Value }).ToList();
	}

	public sealed partial class SimulationHarness
	{
		// C4's hook (design 4.6, 7.4): before IsRunning, after C9's patch. The engine applies the state per day.
		partial void RebuildJobApplyOverride(SimulationRunRequest.Job current, JObject jobManifest)
		{
			SimulationStateOverride.Begin(current, jobManifest, current == null ? null : request.JobDir(current), config);
		}
	}
}
