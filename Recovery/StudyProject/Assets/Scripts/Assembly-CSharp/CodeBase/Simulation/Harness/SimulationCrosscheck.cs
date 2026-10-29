using System.Collections.Generic;
using Codebase.Unit;

namespace Codebase.Simulation.Harness
{
	// DP8 switch and state (design 3.4; unit P11). The job output "crosscheck_dp8" (default off) makes the engine add
	// the spawnerData of each replay/checkpoint override to override_days.jsonl (SimulationSystem.Crosscheck.cs);
	// the RpmFirst running maximum lives here, one per job.
	public static class SimulationCrosscheck
	{
		public const string OutputName = "crosscheck_dp8";

		private static SimulationRunRequest.Job chainJob;
		private static Dictionary<StatType, double> chain;

		public static bool Enabled(SimulationRunRequest.Job job) =>
			job != null && job.Outputs != null && job.Outputs.TryGetValue(OutputName, out bool on) && on;

		// RPMCalculatedValue of the RpmFirst replay so far: OnAwake's zeros at the job's first override.
		public static Dictionary<StatType, double> Chain(SimulationRunRequest.Job job)
		{
			if (chain == null || !ReferenceEquals(chainJob, job))
			{
				chainJob = job;
				chain = new Dictionary<StatType, double>();
			}
			return chain;
		}
	}
}
