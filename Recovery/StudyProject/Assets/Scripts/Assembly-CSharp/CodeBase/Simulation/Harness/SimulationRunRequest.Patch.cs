using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Harness
{
	// Request fields of the config patcher (design 6.1-6.2; unit C9). "patch" is the list of overrides C# applies
	// in memory (Python's PatchReport.expanded: overrides as written plus the generated explicit sets);
	// SimulationConfigPatcher reads it again from the request text with SimulationCanonicalJson, so its numbers
	// are the doubles Python wrote. "patch_spawner_map" names C1's spawner identity map (JSON written by
	// tufu_balance.unity.patcher, absolute or relative to out_dir, checked against sha256), needed by spawner
	// nodes. "patch_on_error": "fail" (default, null) ends the job with exit 5; "record" (empty jobs only, for the
	// shared error cases) writes the error into applied_patch.json and lets the job finish.
	public partial class SimulationRunRequest
	{
		public const string PatchOnErrorFail = "fail", PatchOnErrorRecord = "record";

		public partial class Job
		{
			[JsonProperty("patch", NullValueHandling = NullValueHandling.Ignore)] public JArray Patch;
			[JsonProperty("patch_spawner_map", NullValueHandling = NullValueHandling.Ignore)] public PatchSpawnerMap SpawnerMap;
			[JsonProperty("patch_on_error", NullValueHandling = NullValueHandling.Ignore)] public string PatchOnError;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class PatchSpawnerMap
		{
			[JsonProperty("path", Required = Required.Always)] public string Path;
			[JsonProperty("sha256", Required = Required.Always)] public string Sha256;
		}

		partial void ValidatePatchFields(Job job, ref string error)
		{
			if (error != null) return;
			string where = "job " + job.JobId + ": ";
			if (job.PatchOnError != null && job.PatchOnError != PatchOnErrorFail && job.PatchOnError != PatchOnErrorRecord)
				error = where + "patch_on_error must be fail or record";
			else if (job.PatchOnError == PatchOnErrorRecord && job.Mode != "empty")
				error = where + "patch_on_error = record needs mode empty";
			else if (job.Patch == null && (job.SpawnerMap != null || job.PatchOnError != null))
				error = where + "patch_spawner_map and patch_on_error need a patch";
			else if (job.SpawnerMap != null && (string.IsNullOrEmpty(job.SpawnerMap.Path) || job.SpawnerMap.Sha256 == null ||
			                                    !Regex.IsMatch(job.SpawnerMap.Sha256, "^[0-9a-f]{64}$")))
				error = where + "patch_spawner_map needs a path and a lower-case hex sha256";
		}
	}
}
