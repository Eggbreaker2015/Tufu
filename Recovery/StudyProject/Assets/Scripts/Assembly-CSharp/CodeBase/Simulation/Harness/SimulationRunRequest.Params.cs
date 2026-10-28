using Newtonsoft.Json;

namespace Codebase.Simulation.Harness
{
	// Request field of the parameter loader (design 4.8; unit C3c). "request" (default, also when null): the
	// job's params over the code defaults; "asset": the parameter asset read through the AssetDatabase, the
	// path an interactive run takes. Checked when the engine loads the job (SimulationRebuildParamsLoader).
	public partial class SimulationRunRequest
	{
		public partial class Job
		{
			[JsonProperty("params_source", NullValueHandling = NullValueHandling.Ignore)] public string ParamsSource;
		}
	}
}
