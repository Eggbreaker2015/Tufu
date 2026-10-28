using System;
using System.Collections.Generic;
using Codebase.Simulation.Rebuild;
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

			// Why the params object could not be read exactly (the job then fails with exit 5); null when it was.
			public string ParamsReadError;
		}

		// Newtonsoft reads numbers with the runtime's double parser, which in the editor's Mono rounds some
		// literals 1 ulp off; params feed params_hash and the parameter values, so they are read again from the
		// request text with SimulationCanonicalJson.Parse (Python's json.loads).
		internal void ReadExactParams(string text)
		{
			if (Jobs == null) return;
			List<Newtonsoft.Json.Linq.JObject> exact = null;
			string error = null;
			try
			{
				exact = SimulationCanonicalJson.JobParams(text);
				if (exact.Count != Jobs.Count) error = "the request lists " + exact.Count + " jobs, read " + Jobs.Count;
			}
			catch (Exception e)
			{
				error = e.Message;
			}
			for (int i = 0; i < Jobs.Count; i++)
			{
				if (Jobs[i] == null || Jobs[i].Params == null) continue;
				if (error != null) Jobs[i].ParamsReadError = error;
				else if (exact[i] == null) Jobs[i].ParamsReadError = "params of job " + i + " not found in the request text";
				else Jobs[i].Params = exact[i];
			}
		}
	}
}
