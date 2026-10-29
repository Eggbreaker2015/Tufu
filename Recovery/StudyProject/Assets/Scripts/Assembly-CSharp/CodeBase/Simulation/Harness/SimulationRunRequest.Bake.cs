using Newtonsoft.Json;

namespace Codebase.Simulation.Harness
{
	// Request fields of the bake report (design 1.4.2, 1.4.3, 7.3 C8). A job writes bake_report.json/.csv when
	// outputs.bake_report is true. "bake.snapshots" picks the snapshots the original BuildSpawnerBakeReport reads:
	// "run" (default) this job's own, "shipped" the shipped Simulation Data as on disk (DP5's inputs, in Play).
	public partial class SimulationRunRequest
	{
		public partial class Job
		{
			[JsonProperty("bake", NullValueHandling = NullValueHandling.Ignore)] public BakeOptions Bake;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class BakeOptions
		{
			public const string SnapshotsRun = "run", SnapshotsShipped = "shipped";

			[JsonProperty("snapshots")] public string Snapshots = SnapshotsRun;
		}

		partial void ValidateBakeFields(Job job, ref string error)
		{
			if (error != null) return;
			bool on = SimulationBakeExport.Enabled(job);
			string where = "job " + job.JobId + ": bake";
			string snapshots = job.Bake != null ? job.Bake.Snapshots : BakeOptions.SnapshotsRun;
			if (job.Bake != null && !on) error = where + " is given but outputs." + SimulationBakeExport.OutputName + " is not true";
			else if (!on) return;
			else if (snapshots != BakeOptions.SnapshotsRun && snapshots != BakeOptions.SnapshotsShipped)
				error = where + ".snapshots must be \"run\" or \"shipped\", got " + (snapshots ?? "null");
			else if (snapshots == BakeOptions.SnapshotsRun && job.Mode == "empty")
				error = where + ".snapshots \"run\" needs a job that runs the simulation (mode empty)";
		}
	}
}
