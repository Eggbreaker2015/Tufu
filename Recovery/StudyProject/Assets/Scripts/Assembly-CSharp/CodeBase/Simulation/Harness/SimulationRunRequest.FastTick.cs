using Newtonsoft.Json;

namespace Codebase.Simulation.Harness
{
	// Request fields of the fast phase (unit C5): test-only switches; null in real runs.
	public partial class SimulationRunRequest
	{
		public partial class Job
		{
			[JsonProperty("fast_tick_probe", NullValueHandling = NullValueHandling.Ignore)] public FastTickProbe FastTick;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class FastTickProbe
		{
			// Replace FlushFastTickChunkRewards by the original-shaped reference flush (design 4.5, native replacement
			// acceptance): it reads the five accumulated fields and calls no Rebuild* method. Editor builds only.
			[JsonProperty("reference_flush")] public bool ReferenceFlush;
		}
	}
}
