using Newtonsoft.Json;

namespace Codebase.Simulation.Harness
{
	// Request fields of the slow-phase policy (unit C7): a test-only probe; null in real runs.
	public partial class SimulationRunRequest
	{
		public partial class Job
		{
			[JsonProperty("slow_policy_probe", NullValueHandling = NullValueHandling.Ignore)] public SlowPolicyProbe SlowPolicy;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class SlowPolicyProbe
		{
			// At the run's first FindBestSpawner: the selection on the live spawnerData with equal priorities, with
			// nothing allowed and with nothing eligible (event slow_policy_probe); the run continues unchanged.
			[JsonProperty("selection")] public bool Selection;
		}
	}
}
