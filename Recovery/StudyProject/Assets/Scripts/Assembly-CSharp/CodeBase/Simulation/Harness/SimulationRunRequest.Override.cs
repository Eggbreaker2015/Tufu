using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Codebase.Simulation.Harness
{
	// Request fields of the replay/checkpoint state override (design 1.4.2, 4.6; unit C4). "override" is read by
	// replay and checkpoint jobs (defaults when null) and ignored by free and empty jobs. Python computes what
	// needs inference (items_evolved from P4, slots from the stage-4 fixture or the P12 rule); C# does not.
	public partial class SimulationRunRequest
	{
		public static readonly string[] OverrideModes = { "replay", "checkpoint" };
		public static readonly string[] EconCachesValues = { "stored", "recomputed" };
		public static readonly string[] ReplayItemsValues = { "snapshot", "recompute" };
		public static readonly string[] UnlockValues = { "derived", "snapshot" };
		public static readonly string[] TotalValueValues = { "snapshot", "compose" };

		public partial class Job
		{
			[JsonProperty("override", NullValueHandling = NullValueHandling.Ignore)] public OverrideOptions Override;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class OverrideOptions
		{
			// First day the override applies to; null = 0.
			[JsonProperty("from_day")] public int? FromDay;
			// stored: DpsData/DefenseData caches from the (interpolated) shipped snapshot; recomputed: the recovered
			// CalculateDpsData/CalculateDefenseData from items and slots.
			[JsonProperty("econ_caches")] public string EconCaches = "stored";
			// snapshot: items from the snapshot side that provides them, at every override; recompute: only at the
			// first override, later days keep what the item module accumulates.
			[JsonProperty("replay_items")] public string ReplayItems = "snapshot";
			// derived: islands open through the gates' killability (policy); snapshot: the snapshot's Location
			// (diagnostic).
			[JsonProperty("unlock")] public string Unlock = "derived";
			// snapshot: Stat TotalValue = the (interpolated) snapshot's TotalValue; compose: Value composed with the
			// day's K2/K3 modifiers (what the forced snapshots write).
			[JsonProperty("total_value")] public string TotalValue = "snapshot";
			// {"<shipped TimeChunkIndex>": {"<item Key>": true | false | null}} (tufu_balance.model.items
			// .items_evolved_override). Null or a missing/null entry: evolved iff Level != Copies (counted).
			[JsonProperty("items_evolved")] public Dictionary<string, Dictionary<string, bool?>> ItemsEvolved;
			// {"<shipped TimeChunkIndex>": {"weapon": n, "armor": m}}; null = slots are not overridden.
			[JsonProperty("slots")] public Dictionary<string, SlotCounts> Slots;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class SlotCounts
		{
			[JsonProperty("weapon", Required = Required.Always)] public int Weapon;
			[JsonProperty("armor", Required = Required.Always)] public int Armor;
		}

		internal static bool TryParseDayKey(string key, out int day)
		{
			day = -1;
			return !string.IsNullOrEmpty(key) && key.All(c => c >= '0' && c <= '9') && key.Length <= 6
				&& int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out day)
				&& key == day.ToString(CultureInfo.InvariantCulture);
		}

		partial void ValidateOverrideFields(Job job, ref string error)
		{
			if (error != null || job.Override == null) return;
			OverrideOptions o = job.Override;
			string where = "job " + job.JobId + ": override.";
			if (o.FromDay.HasValue && o.FromDay.Value < 0) error = where + "from_day must be >= 0";
			else if (!EconCachesValues.Contains(o.EconCaches)) error = where + "econ_caches must be one of " + string.Join(", ", EconCachesValues);
			else if (!ReplayItemsValues.Contains(o.ReplayItems)) error = where + "replay_items must be one of " + string.Join(", ", ReplayItemsValues);
			else if (!UnlockValues.Contains(o.Unlock)) error = where + "unlock must be one of " + string.Join(", ", UnlockValues);
			else if (!TotalValueValues.Contains(o.TotalValue)) error = where + "total_value must be one of " + string.Join(", ", TotalValueValues);
			if (error != null) return;
			if (o.ItemsEvolved != null)
				foreach (var pair in o.ItemsEvolved)
				{
					if (!TryParseDayKey(pair.Key, out _)) error = where + "items_evolved key " + pair.Key + " is not a TimeChunkIndex";
					else if (pair.Value == null) error = where + "items_evolved." + pair.Key + " must be an object";
					if (error != null) return;
				}
			if (o.Slots != null)
				foreach (var pair in o.Slots)
				{
					if (!TryParseDayKey(pair.Key, out _)) error = where + "slots key " + pair.Key + " is not a TimeChunkIndex";
					else if (pair.Value == null) error = where + "slots." + pair.Key + " must be an object";
					else if (pair.Value.Weapon < 0 || pair.Value.Armor < 0) error = where + "slots." + pair.Key + " counts must be >= 0";
					if (error != null) return;
				}
		}
	}
}
