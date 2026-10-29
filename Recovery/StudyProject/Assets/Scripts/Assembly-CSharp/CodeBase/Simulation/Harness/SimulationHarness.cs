using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Codebase.ABTests;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Services.Time;
using Codebase.Shop.Systems;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.Harness
{
	// Runtime side of a batch run (design 2.1-2.6; unit C0b). Created by SimulationSandbox when the
	// sandbox is on; runs one job per Play session (reset_mode reenter_play):
	//   WaitReady -> remedy and ready checks (exit 8) -> timeScale 0, settle until the shop RPM pass
	//   is idle -> quiet window (no other system may change health or RPM, exit 6) -> job -> export.
	// The editor driver polls Finished/ExitCode and leaves Play. The heartbeat file is written from
	// Update and from the tick hook, so one long frame of ticks is not a stall.
	public sealed partial class SimulationHarness : MonoBehaviour
	{
		public const int ExitOk = 0, ExitException = 1, ExitGuard = 2, ExitTimeout = 3, ExitNotReady = 4, ExitBadRequest = 5,
			ExitSelfCheck = 6, ExitDevChanged = 7, ExitReadyCheck = 8;

		public const string ManifestSchema = "tufu.sim.manifest/1";
		private const string LogTag = "[TufuSim] ";
		private const double StableReadySeconds = 1.0;
		private const int SettleMinFrames = 10, SettleMaxFrames = 3000, QuietFrames = 30, EmptyJobFrames = 30;
		private const string HeartbeatFile = "heartbeat.json";

		public enum HarnessPhase { None, WaitReady, Settle, Quiet, Job, Done }

		public static HarnessPhase Phase { get; private set; }
		public static bool Finished => Phase == HarnessPhase.Done;
		public static int ExitCode { get; private set; }
		public static string Error { get; private set; }
		public static string StopReason { get; private set; }
		public static long Ticks { get; private set; }
		public static int Frames { get; private set; }

		// Set once max_ticks is reached; the rebuilt ExecuteTick stops on it (wired by C3b).
		public static bool StopRequested { get; private set; }

		// The job of this Play session (null outside batch runs): the engine reads params and engine_probe from it.
		internal static SimulationRunRequest.Job CurrentJob => instance != null ? instance.job : null;

		internal static string RebuildResetMode => instance != null ? instance.request?.Unity.ResetMode : "reenter_play";

		private static SimulationHarness instance;
		private static readonly Stopwatch heartbeatClock = new Stopwatch();
		private static string heartbeatPath;
		private static double heartbeatInterval = 5;
		private static string heartbeatJob;

		private SimulationRunRequest request;
		private SimulationRunRequest.Job job;
		private readonly Stopwatch clock = new Stopwatch();
		private readonly Stopwatch phaseClock = new Stopwatch();
		private readonly Stopwatch frameClock = new Stopwatch();
		private double readyStreakStart = -1;
		private int readyStreakSpawners = -1;
		private int phaseFrames;
		private SimulationConfig config;
		private int speedCap, speedMin, speedMax;
		// SimulationSpeed before the job: restored at the end, so the in-memory config the asset guard digests
		// does not depend on the requested speed or on wall-clock frame pacing (C3d).
		private int? speedBeforeJob;
		private readonly JObject manifest = new JObject();
		private readonly JObject timings = new JObject();
		private readonly List<string> readyRows = new List<string>();
		private string readySnapshot, quietSnapshot;
		private int logErrors, logExceptions;
		// Per-tick wall time of slow ticks and FastTicks (C3b, design 2.7): consecutive ticks of one frame only.
		private readonly Stopwatch tickClock = new Stopwatch();
		private int tickClockFrame = -1;
		private readonly List<double> slowTickMs = new List<double>(), fastTickMs = new List<double>();

		// ---- creation, statics, heartbeat ----

		internal static void Create()
		{
			Phase = HarnessPhase.None;
			ExitCode = ExitOk;
			Error = null;
			StopReason = null;
			Ticks = 0;
			Frames = 0;
			StopRequested = false;
			var go = new GameObject("TufuSimulationHarness") { hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable };
			DontDestroyOnLoad(go);
			instance = go.AddComponent<SimulationHarness>();
		}

		// Called by the rebuilt ExecuteTick after every tick (C3b, RebuildOnTickCompleted). Returns true
		// when the harness wants the run to stop (max_ticks reached).
		public static bool OnTickCompleted()
		{
			Ticks++;
			if (instance != null && instance.job != null && instance.job.Stop.MaxTicks.HasValue && Ticks >= instance.job.Stop.MaxTicks.Value)
				StopRequested = true;
			if (instance != null) instance.SampleTick();
			Beat(false);
			return StopRequested;
		}

		// Heartbeat (design 2.4): the launcher's stall timeout looks at "progress", which changes only
		// when ticks, frames or the phase advance. In the Job phase only ticks count (C3b): a job whose
		// frames run but whose simulation does not tick is a stall.
		public static void Beat(bool force)
		{
			if (heartbeatPath == null) return;
			if (!force && heartbeatClock.IsRunning && heartbeatClock.Elapsed.TotalSeconds < heartbeatInterval) return;
			string progress = (heartbeatJob ?? "-") + ":" + (int)Phase + ":" + Ticks + (Phase == HarnessPhase.Job ? "" : ":" + Frames);
			WriteHeartbeat("play:" + Phase, heartbeatJob, progress, Ticks, Frames);
		}

		public static void ConfigureHeartbeat(string path, double intervalSeconds, string jobId)
		{
			heartbeatPath = path;
			heartbeatInterval = intervalSeconds;
			heartbeatJob = jobId;
		}

		public static void WriteHeartbeat(string phase, string jobId, string progress, long ticks, int frames)
		{
			if (heartbeatPath == null) return;
			try
			{
				var beat = new JObject
				{
					["pid"] = Process.GetCurrentProcess().Id,
					["phase"] = phase,
					["job_id"] = jobId,
					["progress"] = progress,
					["ticks"] = ticks,
					["frames"] = frames,
					["utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
				};
				SimulationJson.WriteText(heartbeatPath, beat.ToString(Newtonsoft.Json.Formatting.None) + "\n");
				heartbeatClock.Restart();
			}
			catch (IOException)
			{
			}
		}

		// ---- partial hooks for later units (design 7.4) ----
		// A partial method has one implementation, so each unit gets its own hook. Called in this order.

		// Before IsRunning: C9 applies the config patch, then C4 the state override.
		partial void RebuildJobApplyPatch(SimulationRunRequest.Job current, JObject jobManifest);

		partial void RebuildJobApplyOverride(SimulationRunRequest.Job current, JObject jobManifest);

		// After the stop, before manifest.json is written: C1 writes the outputs, C8 the bake report.
		partial void RebuildJobExportOutputs(SimulationRunRequest.Job current, string jobDir, JObject jobManifest);

		partial void RebuildJobExportBake(SimulationRunRequest.Job current, string jobDir, JObject jobManifest);

		// After C1's outputs: C3c's daily records, events, digests and manifest "params"/"trace" (SimulationTrace.cs);
		// for a failed job (jobDir null) manifest "params" only.
		partial void RebuildJobExportTrace(SimulationRunRequest.Job current, string jobDir, JObject jobManifest);

		// After the stop, before the export hooks: C2's Play-mode differential probes (job.probes).
		partial void RebuildJobRunProbes(SimulationRunRequest.Job current, string jobDir, JObject jobManifest);

		// ---- lifecycle ----

		private void Awake()
		{
			clock.Start();
			Application.logMessageReceived += OnLog;
			if (SimulationSandbox.ActivationError != null || SimulationSandbox.Request == null)
			{
				Fail(ExitBadRequest, "request rejected in Play: " + (SimulationSandbox.ActivationError ?? "no request"));
				return;
			}
			request = SimulationSandbox.Request;
			job = SimulationSandbox.Job;
			ConfigureHeartbeat(Path.Combine(SimulationSandbox.TempDir, HeartbeatFile), request.Unity.HeartbeatIntervalSeconds, job.JobId);
			manifest["schema"] = ManifestSchema;
			manifest["run_key"] = request.RunKey;
			manifest["code_rev"] = request.CodeRev;
			manifest["job_id"] = job.JobId;
			manifest["job_index"] = SimulationSandbox.JobIndex;
			manifest["mode"] = job.Mode;
			manifest["request"] = SimulationJson.ToJObject(request);
			manifest["job"] = SimulationJson.ToJObject(job);
			manifest["unity"] = new JObject { ["version"] = Application.unityVersion };
			// Kept out of the manifest so runs with and without -nographics stay byte-comparable.
			timings["batchmode"] = Application.isBatchMode;
			timings["graphics_device"] = SystemInfo.graphicsDeviceType.ToString();
			SetPhase(HarnessPhase.WaitReady);
		}

		private void OnDestroy()
		{
			Application.logMessageReceived -= OnLog;
			if (instance == this) instance = null;
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			if (type == LogType.Exception) logExceptions++;
			else if ((type == LogType.Error || type == LogType.Assert) && !message.StartsWith(LogTag)) logErrors++;
		}

		private void SetPhase(HarnessPhase phase)
		{
			Phase = phase;
			phaseFrames = 0;
			phaseClock.Restart();
			timings["t_" + phase] = clock.Elapsed.TotalSeconds;
			Beat(true);
		}

		private void Update()
		{
			if (Phase == HarnessPhase.Done || Phase == HarnessPhase.None) return;
			try
			{
				Frames++;
				phaseFrames++;
				switch (Phase)
				{
					case HarnessPhase.WaitReady:
						WaitReady();
						break;
					case HarnessPhase.Settle:
						Settle();
						break;
					case HarnessPhase.Quiet:
						Quiet();
						break;
					case HarnessPhase.Job:
						StepJob();
						break;
				}
				Beat(false);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Fail(ExitException, e.GetType().Name + ": " + e.Message);
			}
		}

		// ---- readiness ----

		private void WaitReady()
		{
			if (clock.Elapsed.TotalSeconds > request.Unity.ReadyTimeoutSeconds)
			{
				Fail(ExitNotReady, "game not ready after " + request.Unity.ReadyTimeoutSeconds.ToString(CultureInfo.InvariantCulture) + " s: " + DescribeReadiness());
				return;
			}
			if (!IsReady(out int spawners))
			{
				readyStreakStart = -1;
				return;
			}
			double now = clock.Elapsed.TotalSeconds;
			if (readyStreakStart < 0 || spawners != readyStreakSpawners)
			{
				readyStreakStart = now;
				readyStreakSpawners = spawners;
				timings["ready_s"] = now;
				timings["frames_to_ready"] = Frames;
				return;
			}
			if (now - readyStreakStart < StableReadySeconds) return;
			OnReady();
		}

		private static bool IsReady(out int spawners)
		{
			spawners = 0;
			if (SimulationSystem.Instance == null || !GameContext.Has<EnemiesModel>()) return false;
			var model = GameContext.Get<EnemiesModel>();
			spawners = model.Spawners.Count;
			return spawners > 0 && model.PendingBootstrap.Count == 0 && model.PendingEventMobBootstrap.Count == 0;
		}

		private static string DescribeReadiness()
		{
			if (!GameContext.Has<EnemiesModel>()) return "no EnemiesModel; scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			var model = GameContext.Get<EnemiesModel>();
			return "SimulationSystem " + (SimulationSystem.Instance != null) + ", spawners " + model.Spawners.Count +
			       ", pending " + model.PendingBootstrap.Count + ", pending event " + model.PendingEventMobBootstrap.Count;
		}

		private void OnReady()
		{
			config = (SimulationConfig)typeof(SimulationSystem).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SimulationSystem.Instance);
			var failures = new List<string>();
			manifest["sandbox"] = SandboxReport(failures);
			manifest["ab"] = AbReport(failures);
			manifest["ready"] = Remedy(failures);
			if (failures.Count > 0)
			{
				Fail(failures.Any(f => f.StartsWith("guard:")) ? ExitGuard : ExitReadyCheck, string.Join("; ", failures.Take(20)));
				return;
			}
			if (request.Unity.TimeScaleZero) Time.timeScale = 0f;
			if (request.Unity.DisableUi) config.DisableUi = true;
			SetPhase(HarnessPhase.Settle);
		}

		private JObject SandboxReport(List<string> failures)
		{
			var time = GameContext.Get<ITimeService>();
			string pets = SimulationSandbox.PetsState;
			if (pets != "absent" && pets != "disabled") failures.Add("guard: pets " + pets);
			var seeds = new JObject();
			foreach (var seed in request.Sandbox.PrefsSeed.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				JToken seen;
				switch (seed.Value.Type)
				{
					case JTokenType.Integer: seen = PlayerPrefs.GetInt(seed.Key, int.MinValue); break;
					case JTokenType.Float: seen = PlayerPrefs.GetFloat(seed.Key, float.NaN); break;
					default: seen = PlayerPrefs.GetString(seed.Key, null); break;
				}
				seeds[seed.Key] = seen;
			}
			return new JObject
			{
				["identity"] = request.Sandbox.Identity,
				["company_name"] = Application.companyName,
				["product_name"] = Application.productName,
				["persistent_data_path"] = Application.persistentDataPath,
				["prefs_domain"] = "unity." + Application.companyName + "." + Application.productName,
				["clock_epoch_utc"] = request.Sandbox.ClockEpochUtc,
				["time_service_now_utc"] = time.Now.ToDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
				["time_service_cheat_offset_s"] = time.CheatOffset.TotalSeconds,
				["prefs_time_skip_seconds"] = PlayerPrefs.GetFloat("TimeSkipSeconds", 0f),
				["prefs_game_loaded_once"] = PlayerPrefs.GetInt("game_loaded_once", 0),
				["prefs_seed_seen"] = seeds,
				["integrations_skipped"] = request.Sandbox.SkipIntegrations,
				["pets"] = pets,
			};
		}

		private JObject AbReport(List<string> failures)
		{
			var model = GameContext.Get<ABTestModel>();
			var tests = new JObject();
			var lists = new JObject();
			foreach (FieldInfo field in typeof(ABKeys).GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(f => (string)f.GetRawConstantValue(), StringComparer.Ordinal))
			{
				string key = (string)field.GetRawConstantValue();
				string variant = model.GetVariant(key);
				string list = model.GetVariantList(key);
				tests[key] = variant;
				lists[key] = list;
				if (variant != null && list != null && variant != SimulationSandbox.SelectAbVariant(list))
					failures.Add("ab: " + key + " = " + variant + " (variants " + list + ", requested " + request.Sandbox.AbVariant + ")");
				if (variant == null && list != null) failures.Add("ab: " + key + " unassigned");
			}
			return new JObject
			{
				["requested"] = request.Sandbox.AbVariant,
				["tests"] = tests,
				["variant_lists"] = lists,
				["effective"] = new JObject
				{
					["fastRPMParams"] = SimulationJson.ToJObject(ABTestData.FastRPMParams.GetCurrentValue()),
					["spawnerCooldowns"] = SimulationJson.ToJObject(ABTestData.SpawnerCooldowns.GetCurrentValue()),
					["enemyHpDamageMult"] = SimulationJson.ToJObject(ABTestData.EnemyHpDamageMult.GetCurrentValue()),
				},
				["spawner_cooldowns_version"] = ABTestData.SpawnerCooldowns.Version,
			};
		}

		// Spawner readiness remedy (design 2.3, Runbook 6.2): complete any pending bootstrap of a
		// non-event, non-VIP spawner under the frozen clock and re-enable disabled ones; then compare the
		// table with P6's expectation and the frozen AnyoneAlive && IsEnabled count.
		private JObject Remedy(List<string> failures)
		{
			var model = GameContext.Get<EnemiesModel>();
			var time = GameContext.Get<ITimeService>();
			int now = (int)new DateTimeOffset(DateTime.SpecifyKind(time.Now.ToDateTime(), DateTimeKind.Utc)).ToUnixTimeSeconds();
			var sw = Stopwatch.StartNew();
			int bootstrapped = 0, enabled = 0;
			var remedied = new HashSet<EnemySpawner>();
			foreach (EnemySpawner s in model.Spawners)
			{
				if (s.Config.IsEvent || s.Config.IsVip) continue;
				if (!s.IsBootstrapComplete)
				{
					s.CompleteBootstrap(now);
					bootstrapped++;
					remedied.Add(s);
				}
				if (!s.IsEnabled)
				{
					s.SetEnabled(true);
					enabled++;
					remedied.Add(s);
				}
			}
			timings["remedy_ms"] = sw.Elapsed.TotalMilliseconds;

			var rows = new StringBuilder("index,path,key,location,is_event,is_vip,is_dummy,is_elite,item_reward,anyone_alive,is_enabled,count,max_count,bootstrap_complete,charges_left,charges_total,base_cooldown,cooldown,remedied\n");
			var order = new StringBuilder();
			int aliveEnabled = 0, index = 0;
			var exceptions = new JArray();
			foreach (EnemySpawner s in model.Spawners)
			{
				EnemySpawnerConfig c = s.Config;
				string path = HierarchyPath(s.transform);
				bool good = s.AnyoneAlive && s.IsEnabled;
				if (good) aliveEnabled++;
				else exceptions.Add(path + "|" + c.Key);
				order.Append(path).Append('|').Append(c.Key).Append('\n');
				rows.Append(string.Join(",", index++, Csv(path), Csv(c.Key), c.LocationNumber, B(c.IsEvent), B(c.IsVip), B(c.IsDummy), B(c.IsEliteBoss), B(s.HasItemReward),
					B(s.AnyoneAlive), B(s.IsEnabled), s.CurrentCount, s.MaxCount, B(s.IsBootstrapComplete), s.ChargesLeft, s.ChargesTotal,
					SimulationJson.FormatFloat(s.BaseCooldown), SimulationJson.FormatFloat(s.Cooldown), B(remedied.Contains(s)))).Append('\n');
			}
			readyRows.Clear();
			readyRows.Add(rows.ToString());
			int sceneComponents = Resources.FindObjectsOfTypeAll<EnemySpawner>().Count(s => s.gameObject.scene.IsValid());

			int? expected = request.Sandbox.ExpectedAliveEnabled;
			if (expected.HasValue && expected.Value != aliveEnabled)
				failures.Add("ready: AnyoneAlive && IsEnabled = " + aliveEnabled + ", expected " + expected.Value);
			JObject table = CompareExpectedSpawners(model, failures);
			return new JObject
			{
				["registered"] = model.Spawners.Count,
				["scene_spawner_components"] = sceneComponents,
				["enemies"] = model.Enemies.Count,
				["alive_enabled"] = aliveEnabled,
				["expected_alive_enabled"] = expected.HasValue ? (JToken)expected.Value : JValue.CreateNull(),
				["exceptions"] = exceptions,
				["remedy_complete_bootstrap"] = bootstrapped,
				["remedy_enabled"] = enabled,
				["clock_unix"] = now,
				["expected_table"] = table,
				["registration_order_sha256"] = Sha256(order.ToString()),
				["ready_spawners_sha256"] = Sha256(rows.ToString()),
			};
		}

		private JObject CompareExpectedSpawners(EnemiesModel model, List<string> failures)
		{
			string path = request.Sandbox.ExpectedReadySpawners;
			if (string.IsNullOrEmpty(path)) return new JObject { ["checked"] = false };
			// The manifest names the table as the request does (relative to out_dir), so it stays byte-comparable across runs.
			string named = path;
			if (!Path.IsPathRooted(path)) path = Path.Combine(request.OutDir, path);
			if (!File.Exists(path))
			{
				failures.Add("ready: expected_ready_spawners not found: " + path);
				return new JObject { ["checked"] = false, ["path"] = named };
			}
			string[] lines = File.ReadAllLines(path).Where(l => l.Length > 0).ToArray();
			string[] header = SplitCsv(lines[0]);
			int iKey = Array.IndexOf(header, "key"), iPath = Array.IndexOf(header, "path"),
				iCharges = Array.IndexOf(header, "charges_total"), iCooldown = Array.IndexOf(header, "base_cooldown");
			if (iKey < 0 || iCharges < 0 || iCooldown < 0)
			{
				failures.Add("ready: expected_ready_spawners needs key, charges_total, base_cooldown columns");
				return new JObject { ["checked"] = false, ["path"] = named };
			}
			var expected = new Dictionary<string, (int charges, float cooldown)>();
			foreach (string line in lines.Skip(1))
			{
				string[] f = SplitCsv(line);
				string id = iPath >= 0 ? f[iPath] + "|" + f[iKey] : f[iKey];
				expected[id] = (int.Parse(f[iCharges], CultureInfo.InvariantCulture), float.Parse(f[iCooldown], CultureInfo.InvariantCulture));
			}
			int compared = 0, mismatched = 0;
			var details = new JArray();
			var seen = new HashSet<string>();
			foreach (EnemySpawner s in model.Spawners)
			{
				string id = iPath >= 0 ? HierarchyPath(s.transform) + "|" + s.Config.Key : s.Config.Key;
				seen.Add(id);
				if (!expected.TryGetValue(id, out var want))
				{
					mismatched++;
					if (details.Count < 50) details.Add("missing expectation: " + id);
					continue;
				}
				compared++;
				if (want.charges != s.ChargesTotal || BitConverter.SingleToInt32Bits(want.cooldown) != BitConverter.SingleToInt32Bits(s.BaseCooldown))
				{
					mismatched++;
					if (details.Count < 50)
						details.Add(id + ": charges " + s.ChargesTotal + " vs " + want.charges + ", base_cooldown " +
						            SimulationJson.FormatFloat(s.BaseCooldown) + " vs " + SimulationJson.FormatFloat(want.cooldown));
				}
			}
			foreach (string id in expected.Keys.Where(k => !seen.Contains(k)))
			{
				mismatched++;
				if (details.Count < 50) details.Add("not registered: " + id);
			}
			if (mismatched > 0) failures.Add("ready: " + mismatched + " spawner(s) differ from expected_ready_spawners");
			return new JObject { ["checked"] = true, ["path"] = named, ["compared"] = compared, ["mismatched"] = mismatched, ["details"] = details };
		}

		// ---- settle and quiet window ----

		private void Settle()
		{
			if (phaseFrames > SettleMaxFrames)
			{
				Fail(ExitNotReady, "shop RPM pass still running after " + SettleMaxFrames + " frames");
				return;
			}
			if (phaseFrames < SettleMinFrames || ShopRpmBusy()) return;
			timings["settle_frames"] = phaseFrames;
			readySnapshot = QuietState();
			SetPhase(HarnessPhase.Quiet);
		}

		private static bool ShopRpmBusy()
		{
			if (!GameContext.Has<ShopLotRpmCalculationSystem>()) return false;
			FieldInfo busy = typeof(ShopLotRpmCalculationSystem).GetField("isProcessing", BindingFlags.Instance | BindingFlags.NonPublic);
			return busy != null && (bool)busy.GetValue(GameContext.Get<ShopLotRpmCalculationSystem>());
		}

		private void Quiet()
		{
			if (phaseFrames < QuietFrames) return;
			quietSnapshot = QuietState();
			bool same = quietSnapshot == readySnapshot;
			manifest["selfcheck"] = new JObject { ["quiet_frames"] = QuietFrames, ["health_and_rpm_unchanged_quiet"] = same };
			if (!same)
			{
				Fail(ExitSelfCheck, "player health or RPMCalculatedValue changed while frozen");
				return;
			}
			StartJob();
		}

		// Player health and the RPM dictionaries, as IEEE bits: no other system may change them.
		private static string QuietState()
		{
			var sb = new StringBuilder();
			var player = GameContext.Get<PlayerModel>();
			sb.Append("health=").Append(player.Unit != null ? BitConverter.DoubleToInt64Bits(player.Unit.CurrentHealth) : 0).Append('\n');
			var system = SimulationSystem.Instance;
			foreach (var kv in system.RPMCalculatedValue.OrderBy(x => (int)x.Key))
				sb.Append("rpm.").Append((int)kv.Key).Append('=').Append(BitConverter.DoubleToInt64Bits(kv.Value)).Append('\n');
			foreach (var kv in system.SoulRPMCalculatedValue.OrderBy(x => x.Key, StringComparer.Ordinal))
				sb.Append("soul.").Append(kv.Key).Append('=').Append(BitConverter.DoubleToInt64Bits(kv.Value)).Append('\n');
			return sb.ToString();
		}

		// ---- job ----

		private void StartJob()
		{
			var applied = new JObject();
			foreach (var kv in job.ConfigOverrides.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				FieldInfo field = typeof(SimulationConfig).GetField(kv.Key, BindingFlags.Instance | BindingFlags.Public);
				object value = kv.Value.ToObject(field.FieldType);
				field.SetValue(config, value);
				applied[kv.Key] = JToken.FromObject(field.GetValue(config));
			}
			manifest["config_overrides_applied"] = applied;
			config.StopAtDay = job.Stop.Day.HasValue;
			config.TargetDay = job.Stop.Day ?? 0;
			config.StopAtLocation = job.Stop.Location.HasValue;
			config.TargetLocationNumber = job.Stop.Location ?? 0;
			speedBeforeJob = config.SimulationSpeed;
			config.SimulationSpeed = request.Unity.SimulationSpeed;
			speedCap = Math.Max(1, request.Unity.SimulationSpeed) * 16;
			speedMin = speedMax = config.SimulationSpeed;

			// S1's in-session reset (design 7.4); false while S1 is absent, when only a fresh Play session resets.
			if (!SimulationSystem.Instance.RebuildTryResetForJob() && request.Unity.ResetMode != "reenter_play")
			{
				Fail(ExitBadRequest, "reset_mode " + request.Unity.ResetMode + " needs S1's RebuildResetForJob");
				return;
			}
			int zeroed = 0;
			if (request.Sandbox.ZeroReadyCooldowns)
			{
				foreach (EnemySpawner s in GameContext.Get<EnemiesModel>().Spawners)
				{
					if (s.Config.IsEvent || s.Config.IsVip || s.Cooldown == 0f) continue;
					s.Cooldown = 0f;
					zeroed++;
				}
			}
			manifest["ready_cooldowns_zeroed"] = zeroed;
			manifest["state_sha256_job_start"] = Sha256(StateDigestText());
			SimulationExport.ResetRecorder(); // no snapshot record carries over from an earlier job (design 2.6)
			RebuildJobApplyPatch(job, manifest);
			RebuildJobApplyOverride(job, manifest);
			SetPhase(HarnessPhase.Job);
			Ticks = 0;
			StopRequested = false;
			if (job.Mode != "empty") config.IsRunning = true;
			frameClock.Restart();
			RunTestActions();
		}

		private void RunTestActions()
		{
			SimulationRunRequest.HarnessTest test = job.Test;
			if (test == null) return;
			if (test.MutatePlayerConfig)
			{
				foreach (PlayerConfig pc in Resources.FindObjectsOfTypeAll<PlayerConfig>())
					if (pc.Stats != null && pc.Stats.Count > 0)
						pc.Stats[0].Modifiers.Add(new StatModifier { Name = "TufuSimGuardTest", StatType = pc.Stats[0].Type, Value = 1f });
			}
			if (test.BusySeconds > 0)
			{
				var sw = Stopwatch.StartNew();
				while (sw.Elapsed.TotalSeconds < test.BusySeconds)
				{
					if (test.BusyTicks) OnTickCompleted();
					System.Threading.Thread.Sleep(1);
				}
			}
			if (test.ThrowException) throw new InvalidOperationException("harness test exception");
		}

		private void StepJob()
		{
			double frameMs = frameClock.Elapsed.TotalMilliseconds;
			frameClock.Restart();
			SimulationRunRequest.StopOptions stop = job.Stop;
			SimulationSystem engine = SimulationSystem.Instance;
			if (job.Mode != "empty" && engine != null && engine.RebuildFailure != null)
			{
				manifest["engine"] = engine.RebuildManifest();
				Fail(engine.RebuildFailureExitCode, engine.RebuildFailure);
				return;
			}
			string reason = null;
			if (job.Mode == "empty")
			{
				if (phaseFrames >= (stop.MaxFrames ?? EmptyJobFrames)) reason = "max_frames";
			}
			else if (!config.IsRunning) reason = "simulation_stopped";
			if (reason == null && StopRequested) reason = "max_ticks";
			if (reason == null && stop.MaxFrames.HasValue && phaseFrames >= stop.MaxFrames.Value) reason = "max_frames";
			if (reason == null && stop.MaxWallSeconds.HasValue && phaseClock.Elapsed.TotalSeconds >= stop.MaxWallSeconds.Value) reason = "max_wall_seconds";
			if (reason != null)
			{
				FinishJob(reason);
				return;
			}
			// Frame budget (design 2.4): only throughput changes, results do not depend on the speed.
			if (config.IsRunning && phaseFrames > 1)
			{
				double budget = request.Unity.FrameBudgetMs;
				if (frameMs > budget) config.SimulationSpeed = Math.Max(1, (int)(config.SimulationSpeed * budget / frameMs));
				else if (frameMs < budget / 2) config.SimulationSpeed = Math.Min(speedCap, config.SimulationSpeed * 2);
				speedMin = Math.Min(speedMin, config.SimulationSpeed);
				speedMax = Math.Max(speedMax, config.SimulationSpeed);
			}
		}

		private void FinishJob(string reason)
		{
			config.IsRunning = false;
			StopReason = reason;
			timings["job_wall_s"] = phaseClock.Elapsed.TotalSeconds;
			timings["job_frames"] = phaseFrames;
			timings["simulation_speed_min"] = speedMin;
			timings["simulation_speed_max"] = speedMax;
			manifest["stop_reason"] = reason;
			manifest["counts"] = new JObject { ["ticks"] = Ticks };
			if (job.Mode != "empty" && SimulationSystem.Instance != null) manifest["engine"] = SimulationSystem.Instance.RebuildManifest();
			timings["slow_tick_ms"] = TickStats(slowTickMs);
			timings["fast_tick_ms"] = TickStats(fastTickMs);
			if (job.Mode == "empty")
			{
				bool same = QuietState() == quietSnapshot;
				((JObject)manifest["selfcheck"])["health_and_rpm_unchanged_job"] = same;
				if (!same)
				{
					Fail(ExitSelfCheck, "player health or RPMCalculatedValue changed during the empty job");
					return;
				}
			}
			manifest["state_sha256_job_end"] = Sha256(StateDigestText());
			string dir = request.JobDir(job);
			Directory.CreateDirectory(dir);
			RebuildJobRunProbes(job, dir, manifest);
			RebuildJobExportOutputs(job, dir, manifest);
			RebuildJobExportTrace(job, dir, manifest);
			RebuildJobExportBake(job, dir, manifest);
			Complete(ExitOk, null);
		}

		private void SampleTick()
		{
			bool fast = SimulationSystem.Instance != null && SimulationSystem.Instance.RebuildLastTickWasFast;
			if (tickClockFrame == Frames && tickClock.IsRunning) (fast ? fastTickMs : slowTickMs).Add(tickClock.Elapsed.TotalMilliseconds);
			tickClockFrame = Frames;
			tickClock.Restart();
		}

		private static JObject TickStats(List<double> samples)
		{
			var stats = new JObject { ["n"] = samples.Count };
			if (samples.Count == 0) return stats;
			var sorted = samples.OrderBy(x => x).ToList();
			stats["mean"] = sorted.Average();
			stats["p50"] = sorted[sorted.Count / 2];
			stats["p95"] = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.95))];
			stats["max"] = sorted[sorted.Count - 1];
			return stats;
		}

		// ---- state digest (design 2.6, harness subset; C3c adds per-day digests) ----

		private static string StateDigestText()
		{
			var sb = new StringBuilder();
			var player = GameContext.Get<PlayerModel>();
			if (player.Unit != null)
			{
				foreach (Stat stat in player.Unit.Stats.OrderBy(s => (int)s.Type))
					sb.Append("stat.").Append((int)stat.Type).Append('=').Append(BitConverter.DoubleToInt64Bits(stat.Value)).Append(',')
						.Append(BitConverter.DoubleToInt64Bits(stat.SpecificValue)).Append('\n');
				sb.Append("health=").Append(BitConverter.DoubleToInt64Bits(player.Unit.CurrentHealth)).Append('\n');
			}
			foreach (ItemModel item in player.Items.OrderBy(i => i.Key, StringComparer.Ordinal).ThenBy(i => i.Level).ThenBy(i => i.Copies))
				sb.Append("item.").Append(item.Key).Append('=').Append(item.Level).Append(',').Append(item.Copies).Append(',').Append(B(item.Evolved)).Append('\n');
			sb.Append("location=").Append(player.Data.CurrentLocationNumber.Value).Append(',').Append(player.Data.UnlockedLocationNumber.Value).Append('\n');
			var system = SimulationSystem.Instance;
			foreach (var kv in system.RPMCalculatedValue.OrderBy(x => (int)x.Key))
				sb.Append("rpm.").Append((int)kv.Key).Append('=').Append(BitConverter.DoubleToInt64Bits(kv.Value)).Append('\n');
			int index = 0;
			foreach (EnemySpawner s in GameContext.Get<EnemiesModel>().Spawners)
				sb.Append("spawner.").Append(index++).Append('=').Append(s.Config.Key).Append(',').Append(s.ChargesLeft).Append(',')
					.Append(BitConverter.SingleToInt32Bits(s.Cooldown)).Append(',').Append(s.CurrentCount).Append('\n');
			var ab = GameContext.Get<ABTestModel>();
			foreach (FieldInfo field in typeof(ABKeys).GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(f => (string)f.GetRawConstantValue(), StringComparer.Ordinal))
			{
				string key = (string)field.GetRawConstantValue();
				sb.Append("ab.").Append(key).Append('=').Append(ab.GetVariant(key) ?? "null").Append('\n');
			}
			return sb.ToString();
		}

		// ---- completion ----

		private void Fail(int code, string message)
		{
			Debug.LogError(LogTag + "job failed (exit " + code + "): " + message);
			if (config != null) config.IsRunning = false;
			Complete(code, message);
		}

		private void Complete(int code, string error)
		{
			if (Phase == HarnessPhase.Done) return;
			ExitCode = code;
			Error = error;
			manifest["exit_code"] = code;
			manifest["error"] = error;
			if (SimulationSystem.Instance != null && job != null && job.Mode != "empty")
			{
				bool inventoryProbe = job.Outputs != null && job.Outputs.TryGetValue("inventory", out bool inventoryOn) && inventoryOn;
				JObject inventory = SimulationSystem.Instance.RebuildFinishInventory(inventoryProbe);
				if (request != null && inventoryProbe)
				{
					string inventoryDir = request.JobDir(job);
					Directory.CreateDirectory(inventoryDir);
					SimulationJson.WriteText(Path.Combine(inventoryDir, "inventory.json"), inventory.ToString(Newtonsoft.Json.Formatting.None) + "\n");
				}
			}
			if (config != null && speedBeforeJob.HasValue) config.SimulationSpeed = speedBeforeJob.Value;
			timings["log_errors"] = logErrors;
			timings["log_exceptions"] = logExceptions;
			timings["t_done"] = clock.Elapsed.TotalSeconds;
			try
			{
				if (request != null && job != null)
				{
					string dir = request.JobDir(job);
					Directory.CreateDirectory(dir);
					if (code != ExitOk) RebuildJobExportTrace(job, null, manifest);
					if (readyRows.Count > 0) SimulationJson.WriteText(Path.Combine(dir, "ready_spawners.csv"), readyRows[0]);
					SimulationJson.WriteText(Path.Combine(dir, "manifest.json"), manifest.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n");
					SimulationJson.WriteText(Path.Combine(dir, "timings.json"), timings.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n");
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				if (ExitCode == ExitOk) ExitCode = ExitException;
			}
			Phase = HarnessPhase.Done;
			Beat(true);
			Debug.Log(LogTag + "job " + (job != null ? job.JobId : "?") + " done: exit " + ExitCode + (StopReason != null ? ", stop " + StopReason : ""));
		}

		// ---- helpers ----

		internal static string HierarchyPath(Transform t)
		{
			var names = new List<string>();
			for (Transform x = t; x != null; x = x.parent) names.Add(x.name);
			names.Reverse();
			return t.gameObject.scene.name + ":" + string.Join("/", names);
		}

		private static string B(bool value) => value ? "1" : "0";

		private static string Csv(string s) => s == null ? "" : s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

		private static string[] SplitCsv(string line)
		{
			var fields = new List<string>();
			var cur = new StringBuilder();
			bool quoted = false;
			for (int i = 0; i < line.Length; i++)
			{
				char ch = line[i];
				if (quoted)
				{
					if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
					else if (ch == '"') quoted = false;
					else cur.Append(ch);
				}
				else if (ch == '"') quoted = true;
				else if (ch == ',') { fields.Add(cur.ToString()); cur.Clear(); }
				else cur.Append(ch);
			}
			fields.Add(cur.ToString());
			return fields.ToArray();
		}

		internal static string Sha256(string text)
		{
			using (SHA256 sha = SHA256.Create())
				return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
		}
	}
}
