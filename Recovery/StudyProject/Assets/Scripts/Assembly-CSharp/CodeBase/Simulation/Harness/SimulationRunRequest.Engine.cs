using Newtonsoft.Json;

namespace Codebase.Simulation.Harness
{
	// Request fields of the engine (unit C3b): test-only switches for engine checks; null in real runs.
	public partial class SimulationRunRequest
	{
		public partial class Job
		{
			[JsonProperty("engine_probe", NullValueHandling = NullValueHandling.Ignore)] public EngineProbe Engine;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class EngineProbe
		{
			// FindBestSpawner is not called and FastTick is the minimal one (RebuildAdvanceFastDay(0)), whatever
			// the module units have filled in: the engine's own behaviour under an empty policy.
			[JsonProperty("null_policy")] public bool NullPolicy;
			// Unlock islands 2..N through RebuildUnlockLocation before the run starts (per-tick cost with every
			// island open). 0 = off.
			[JsonProperty("unlock_through")] public int UnlockThrough;
			// Run the write-point scenario of design 7.3 C3b (RebuildApplyClear, DAFC, gates) right after
			// PrepareSimulationRun, record it in manifest engine.probe and stop.
			[JsonProperty("write_points")] public bool WritePoints;
		}
	}
}
