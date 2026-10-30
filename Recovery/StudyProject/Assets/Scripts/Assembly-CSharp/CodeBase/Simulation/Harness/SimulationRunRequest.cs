using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Harness
{
	// Batch request, core fields (design 1.4.2; unit C0b). Other units add fields in
	// SimulationRunRequest.<Unit>.cs as partial members of these classes. Fields are only ever added;
	// a change of meaning bumps Schema. Unknown members fail parsing (SimulationJson.ReadSettings).
	[JsonObject(MemberSerialization.OptIn)]
	public partial class SimulationRunRequest
	{
		public const string SchemaId = "tufu.sim.request/1";
		public const string SandboxCompany = "TufuSimSandbox";

		public static readonly string[] Modes = { "free", "checkpoint", "replay", "empty" };

		// SimulationConfig members a request may change in memory (run switches only).
		public static readonly string[] ConfigOverrideWhitelist =
		{
			"UseSpawnerCharges", "SpawnerChargesChunkMinutes", "TimeChunkDurationMinutes", "CrystalSimulation", "GraphSimulation",
			"AutoGambling", "AutoBalanceItems", "AutoBalanceAmulet", "IsLog", "IsSmartPath", "IsDynamicResourcePriority",
			"IsCapOptimumTime", "TrimSimulationData", "TimeStep", "SnapshotInterval", "UseFastTick", "FastTickStartChunkOffset",
			"EarlySpawnerOptimumTimeOffsetStartLocation", "EarlySpawnerOptimumTimeOffsetRelativePart",
			"EarlySpawnerOptimumTimeOffsetMultiplier",
		};

		[JsonProperty("schema", Required = Required.Always)] public string Schema;
		[JsonProperty("run_key")] public string RunKey;
		[JsonProperty("code_rev")] public string CodeRev;
		[JsonProperty("out_dir", Required = Required.Always)] public string OutDir;
		[JsonProperty("unity")] public UnityOptions Unity = new UnityOptions();
		[JsonProperty("sandbox")] public SandboxOptions Sandbox = new SandboxOptions();
		[JsonProperty("jobs", Required = Required.Always)] public List<Job> Jobs = new List<Job>();

		[JsonObject(MemberSerialization.OptIn)]
		public partial class UnityOptions
		{
			[JsonProperty("simulation_speed")] public int SimulationSpeed = 500;
			[JsonProperty("frame_budget_ms")] public double FrameBudgetMs = 1000;
			[JsonProperty("time_scale_zero")] public bool TimeScaleZero = true;
			[JsonProperty("disable_ui")] public bool DisableUi = true;
			[JsonProperty("reset_mode")] public string ResetMode = "reenter_play";
			[JsonProperty("ready_timeout_s")] public double ReadyTimeoutSeconds = 300;
			[JsonProperty("heartbeat_interval_s")] public double HeartbeatIntervalSeconds = 5;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class SandboxOptions
		{
			[JsonProperty("ab_variant")] public string AbVariant = "A";
			[JsonProperty("pets")] public string Pets = "absent_or_disabled";
			[JsonProperty("clock_epoch_utc")] public string ClockEpochUtc = "2024-01-01T00:00:00Z";
			[JsonProperty("identity")] public string Identity = SandboxCompany;
			// Values: JSON integer -> SetInt, JSON float -> SetFloat, string -> SetString.
			[JsonProperty("prefs_seed")] public Dictionary<string, JToken> PrefsSeed = new Dictionary<string, JToken>();
			// P6's table of ChargesTotal/BaseCooldown per runtime spawner; null = export only.
			[JsonProperty("expected_ready_spawners")] public string ExpectedReadySpawners;
			// Frozen count of registered spawners with AnyoneAlive && IsEnabled after the remedy; null = no check.
			[JsonProperty("expected_alive_enabled")] public int? ExpectedAliveEnabled;
			[JsonProperty("skip_integrations")] public bool SkipIntegrations = true;
			// Ready spawners carry Cooldown = seconds to the next UTC-aligned processing (Runbook 6.1), which
			// the recovered bot filter reads as "not respawned"; zero it before IsRunning.
			[JsonProperty("zero_ready_cooldowns")] public bool ZeroReadyCooldowns = true;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class Job
		{
			[JsonProperty("job_id", Required = Required.Always)] public string JobId;
			[JsonProperty("mode")] public string Mode = "free";
			[JsonProperty("stop")] public StopOptions Stop = new StopOptions();
			[JsonProperty("config_overrides")] public Dictionary<string, JToken> ConfigOverrides = new Dictionary<string, JToken>();
			[JsonProperty("params")] public JObject Params;
			[JsonProperty("params_hash")] public string ParamsHash;
			[JsonProperty("outputs")] public Dictionary<string, bool> Outputs = new Dictionary<string, bool>();
			// Harness self-test actions; null in real runs.
			[JsonProperty("test")] public HarnessTest Test;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class StopOptions
		{
			[JsonProperty("day")] public int? Day = 405;
			[JsonProperty("location")] public int? Location;
			[JsonProperty("max_ticks")] public long? MaxTicks = 5000000;
			[JsonProperty("max_wall_seconds")] public double? MaxWallSeconds = 900;
			[JsonProperty("max_frames")] public int? MaxFrames;
		}

		[JsonObject(MemberSerialization.OptIn)]
		public partial class HarnessTest
		{
			// Adds a modifier to PlayerConfig.Stats[0].Modifiers in Play (asset guard test).
			[JsonProperty("mutate_player_config")] public bool MutatePlayerConfig;
			// The editor skips the forced reimport after this job, so the guard must fail (exit 2).
			[JsonProperty("skip_reimport")] public bool SkipReimport;
			// One frame busy for this long; with busy_ticks the tick hook keeps the heartbeat going.
			[JsonProperty("busy_seconds")] public double BusySeconds;
			[JsonProperty("busy_ticks")] public bool BusyTicks;
			[JsonProperty("throw_exception")] public bool ThrowException;
		}

		public static SimulationRunRequest Load(string path)
		{
			string text = File.ReadAllText(path, System.Text.Encoding.UTF8);
			SimulationRunRequest request = SimulationJson.Read<SimulationRunRequest>(text);
			request.ReadExactParams(text); // C3c (SimulationRunRequest.Params.cs)
			return request;
		}

		// Returns null when valid, else the reason (exit code 5).
		public string Validate()
		{
			if (Schema != SchemaId) return "schema must be " + SchemaId + ", got " + Schema;
			if (string.IsNullOrEmpty(OutDir) || !Path.IsPathRooted(OutDir)) return "out_dir must be an absolute path";
			if (Unity == null || Sandbox == null) return "unity and sandbox must be objects";
			if (Unity.ResetMode != "reenter_play") return "reset_mode " + Unity.ResetMode + " is not supported (only reenter_play)";
			if (Unity.SimulationSpeed < 1) return "simulation_speed must be >= 1";
			if (!(Unity.FrameBudgetMs > 0)) return "frame_budget_ms must be > 0";
			if (!(Unity.ReadyTimeoutSeconds > 0)) return "ready_timeout_s must be > 0";
			if (!(Unity.HeartbeatIntervalSeconds > 0)) return "heartbeat_interval_s must be > 0";
			if (string.IsNullOrEmpty(Sandbox.Identity) || !Regex.IsMatch(Sandbox.Identity, "^TufuSim[A-Za-z0-9_-]{0,48}$"))
				return "sandbox.identity must match ^TufuSim[A-Za-z0-9_-]*$";
			if (!TryParseClock(Sandbox.ClockEpochUtc, out _)) return "sandbox.clock_epoch_utc is not an ISO-8601 UTC time: " + Sandbox.ClockEpochUtc;
			if (string.IsNullOrEmpty(Sandbox.AbVariant)) return "sandbox.ab_variant must be set";
			if (Sandbox.Pets != "absent_or_disabled") return "sandbox.pets must be absent_or_disabled";
			if (Sandbox.PrefsSeed == null) return "sandbox.prefs_seed must be an object";
			foreach (var seed in Sandbox.PrefsSeed)
			{
				JTokenType t = seed.Value == null ? JTokenType.Null : seed.Value.Type;
				if (t != JTokenType.Integer && t != JTokenType.Float && t != JTokenType.String)
					return "sandbox.prefs_seed." + seed.Key + " must be an integer, float or string";
				if (t == JTokenType.Integer && (seed.Value.Value<long>() > int.MaxValue || seed.Value.Value<long>() < int.MinValue))
					return "sandbox.prefs_seed." + seed.Key + " is out of int range";
			}
			if (Jobs == null || Jobs.Count == 0) return "jobs must not be empty";
			var ids = new HashSet<string>();
			foreach (Job job in Jobs)
			{
				if (job == null) return "null job";
				if (string.IsNullOrEmpty(job.JobId) || !Regex.IsMatch(job.JobId, "^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$"))
					return "job_id must match ^[A-Za-z0-9][A-Za-z0-9_.-]*$: " + job.JobId;
				if (!ids.Add(job.JobId)) return "duplicate job_id " + job.JobId;
				if (!Modes.Contains(job.Mode)) return "job " + job.JobId + ": unknown mode " + job.Mode;
				if (job.Stop == null) return "job " + job.JobId + ": stop must be an object";
				if (job.ConfigOverrides == null) return "job " + job.JobId + ": config_overrides must be an object";
				foreach (var key in job.ConfigOverrides.Keys)
					if (!ConfigOverrideWhitelist.Contains(key)) return "job " + job.JobId + ": config_overrides." + key + " is not a run switch";
				if (job.Outputs == null) return "job " + job.JobId + ": outputs must be an object";
				string error = null;
				ValidateOverrideFields(job, ref error);
				ValidateBakeFields(job, ref error);
				ValidatePatchFields(job, ref error);
				if (error != null) return error;
			}
			return null;
		}

		// Validation of the fields C4 (override), C8 (bake) and C9 (patch) add in their partial files.
		partial void ValidateOverrideFields(Job job, ref string error);

		partial void ValidateBakeFields(Job job, ref string error);

		partial void ValidatePatchFields(Job job, ref string error);

		public static bool TryParseClock(string text, out DateTime utc)
		{
			utc = default;
			if (string.IsNullOrEmpty(text)) return false;
			if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime value))
				return false;
			utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
			return true;
		}

		public string JobDir(Job job) => Path.Combine(OutDir, job.JobId);
	}
}
