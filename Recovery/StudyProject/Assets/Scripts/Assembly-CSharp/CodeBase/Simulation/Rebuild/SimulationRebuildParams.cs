using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Rebuild
{
	// Fractional item counts (design 4.7): a per-stream accumulator, or SplitMix64 draws.
	public enum RollMode
	{
		Carry = 0,
		Seeded = 1
	}

	// Day-change order (design 4.4, P07). The data fixes RpmFirst; the others are ablations only.
	public enum DpsCacheProvenance
	{
		// RPM and DpsData under K2/K3(d-1), then the K(d) modifiers, then the forced snapshot.
		RpmFirst = 0,
		// K(d) modifiers first, then RPM and DpsData.
		ModifierFirst = 1,
		// RPM under K2/K3(d-1) from the last tick's DpsData/DefenseData caches (not recomputed).
		StaleTickCache = 2
	}

	// Evolution tables (design 4.5, Q16): the shipped tables, or the original co-evolution rules.
	public enum AutoBalanceMode
	{
		Frozen = 0,
		Regenerate = 1
	}

	// Calibratable parameters of the rebuilt simulator (design 4.8); core fields with code defaults (unit C3b).
	// Module units add fields in SimulationRebuildParams.<Module>.cs; defaults mirror Balance/sim/params/defaults/*.json.
	// Field types stay JsonUtility-compatible (int, float, double, bool, string, enums and their arrays).
	[Serializable]
	public partial class SimulationRebuildParams
	{
		public int SchemaVersion = 1;
		public RollMode RollMode = RollMode.Carry;
		public int Seed = 1;
		public DpsCacheProvenance DpsCacheProvenance = DpsCacheProvenance.RpmFirst;
		// P06: early-list cadence; EarlySnapshotIntervals[i] minutes applies from P >= EarlySnapshotIntervalStarts[i].
		public float[] EarlySnapshotIntervalStarts = { 0f, 20f };
		public float[] EarlySnapshotIntervals = { 1f, 5f };
		// P05: fast-phase offline step until the day index changes.
		public float FastAfkStepMinutes = 120f;
		public AutoBalanceMode AutoBalanceMode = AutoBalanceMode.Frozen;

		// The request's params object over the code defaults; unknown members are errors. C3c adds the
		// parameter asset and the full request override.
		internal static SimulationRebuildParams FromJson(JObject json)
		{
			var result = new SimulationRebuildParams();
			if (json == null) return result;
			var settings = new JsonSerializerSettings
			{
				MissingMemberHandling = MissingMemberHandling.Error,
				ObjectCreationHandling = ObjectCreationHandling.Replace,
			};
			JsonConvert.PopulateObject(json.ToString(Formatting.None), result, settings);
			return result;
		}

		// Null when usable, else the reason.
		internal string Validate()
		{
			if (SchemaVersion != 1) return "params.SchemaVersion " + SchemaVersion + " is not supported";
			if (!Enum.IsDefined(typeof(DpsCacheProvenance), DpsCacheProvenance)) return "params.DpsCacheProvenance " + (int)DpsCacheProvenance + " is unknown";
			if (!Enum.IsDefined(typeof(RollMode), RollMode)) return "params.RollMode " + (int)RollMode + " is unknown";
			if (!Enum.IsDefined(typeof(AutoBalanceMode), AutoBalanceMode)) return "params.AutoBalanceMode " + (int)AutoBalanceMode + " is unknown";
			if (EarlySnapshotIntervalStarts == null || EarlySnapshotIntervals == null || EarlySnapshotIntervalStarts.Length == 0
			    || EarlySnapshotIntervalStarts.Length != EarlySnapshotIntervals.Length || EarlySnapshotIntervalStarts[0] != 0f)
				return "params.EarlySnapshotIntervalStarts must start at 0 and match EarlySnapshotIntervals";
			for (int i = 0; i < EarlySnapshotIntervals.Length; i++)
			{
				if (!(EarlySnapshotIntervals[i] > 0f)) return "params.EarlySnapshotIntervals must be > 0";
				if (i > 0 && !(EarlySnapshotIntervalStarts[i] > EarlySnapshotIntervalStarts[i - 1])) return "params.EarlySnapshotIntervalStarts must increase";
			}
			if (!(FastAfkStepMinutes > 0f)) return "params.FastAfkStepMinutes must be > 0";
			return null;
		}
	}
}
