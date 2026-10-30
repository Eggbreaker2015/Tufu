using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Codebase.Simulation.Harness;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.EditorTools
{
	// Editor half of a batch run (design 2.1-2.5; unit C0b). State lives in SessionState and survives the
	// domain reload of every Play entry; [InitializeOnLoad] resumes it when the stored process id is ours.
	//   setup: Enter Play Mode guard, asset guard baseline, PlayerSettings rename into the sandbox
	//   per job: clear sandbox save + PlayerPrefs, seed prefs, write Temp/TufuSimulation/active_request.json,
	//            enter Play from Boot, wait for SimulationHarness, leave Play, asset guard
	//   end: PlayerPrefs.Save() in the sandbox domain, restore the names, EditorApplication.Exit(code)
	// Serve mode repeats the per-job part for requests dropped into a queue directory.
	[InitializeOnLoad]
	public static class SimulationPlayModeDriver
	{
		private const string P = "TufuSim.";
		private const string LogTag = "[TufuSim] ";
		private const string ProbeKey = "tufu_sandbox_probe";
		private const double HarnessStartTimeoutSeconds = 120, JobSlackSeconds = 600;

		private static bool hooked;
		private static long editTicks;
		private static readonly Stopwatch beatClock = new Stopwatch();
		private static double lastInboxPoll;

		static SimulationPlayModeDriver()
		{
			if (!IsActive()) return;
			Hook();
		}

		// ---- entries ----

		public static void BeginRun(string entry, string requestPath)
		{
			ResetSession();
			SessionState.SetString(P + "mode", "run");
			SessionState.SetString(P + "entry", entry);
			requestPath = Path.GetFullPath(requestPath);
			JObject report = NewReport(entry);
			report["request_path"] = requestPath;
			SaveReport(report);
			SessionState.SetString(P + "out_dir", Path.GetDirectoryName(requestPath));
			SimulationRunRequest request;
			try
			{
				request = SimulationRunRequest.Load(requestPath);
			}
			catch (Exception e)
			{
				Debug.LogError(LogTag + "invalid request " + requestPath + ": " + e.Message);
				Report("request_error", e.GetType().Name + ": " + e.Message);
				Finish(SimulationBatch.ExitBadRequest);
				return;
			}
			string error = request.Validate();
			if (error != null)
			{
				Debug.LogError(LogTag + "invalid request: " + error);
				Report("request_error", error);
				Finish(SimulationBatch.ExitBadRequest);
				return;
			}
			Directory.CreateDirectory(request.OutDir);
			SessionState.SetString(P + "out_dir", request.OutDir);
			SessionState.SetString(P + "request_path", requestPath);
			SessionState.SetString(P + "code_rev", request.CodeRev ?? "");
			SessionState.SetFloat(P + "beat_interval", (float)request.Unity.HeartbeatIntervalSeconds);
			if (!Setup(request.Sandbox.Identity)) return;
			SessionState.SetInt(P + "job_index", 0);
			SessionState.SetString(P + "job_results", "[]");
			Hook();
			StartJob();
		}

		public static void BeginServe(string configPath)
		{
			ResetSession();
			SessionState.SetString(P + "mode", "serve");
			SessionState.SetString(P + "entry", "serve");
			JObject report = NewReport("serve");
			JObject config = JObject.Parse(File.ReadAllText(configPath));
			string queue = (string)config["queue_dir"];
			string identity = (string)config["identity"] ?? SimulationRunRequest.SandboxCompany;
			report["serve_config"] = config;
			SaveReport(report);
			SessionState.SetString(P + "queue_dir", queue);
			// Unity deletes Temp/ (and the queue in it) when it exits: the report goes to report_dir.
			SessionState.SetString(P + "out_dir", (string)config["report_dir"] ?? queue);
			SessionState.SetString(P + "code_rev", (string)config["code_rev"] ?? "");
			SessionState.SetString(P + "identity", identity);
			SessionState.SetFloat(P + "idle_timeout", (float)((double?)config["idle_timeout_s"] ?? 1800));
			SessionState.SetFloat(P + "beat_interval", (float)((double?)config["heartbeat_interval_s"] ?? 5));
			foreach (string sub in new[] { "inbox", "running", "done" }) Directory.CreateDirectory(Path.Combine(queue, sub));
			if (!Setup(identity)) return;
			SessionState.SetString(P + "stage", "idle");
			SessionState.SetString(P + "last_activity", Now());
			Hook();
			Debug.Log(LogTag + "serving " + queue + " at " + SessionState.GetString(P + "code_rev", ""));
		}

		public static void Abort(int code)
		{
			if (!IsActive()) EditorApplication.Exit(code);
			else Finish(code);
		}

		// ---- setup and sandbox ----

		private static bool Setup(string identity)
		{
			JObject report = LoadReport();
			report["enter_play_mode_options"] = EditorSettings.enterPlayModeOptions.ToString();
			// 6000.6 reads enterPlayModeOptionsEnabled as False even when the asset says 1 (Runbook 3.7):
			// only the Disable* flags decide.
			bool reloads = (EditorSettings.enterPlayModeOptions & (EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload)) == 0;
			report["domain_and_scene_reload_on"] = reloads;
			SaveReport(report);
			if (!reloads)
			{
				Debug.LogError(LogTag + "Enter Play Mode options disable domain or scene reload");
				Finish(SimulationBatch.ExitGuard);
				return false;
			}
			var sw = Stopwatch.StartNew();
			Dictionary<string, string> memory = SimulationAssetGuard.MemoryDigests();
			Dictionary<string, string> disk = SimulationAssetGuard.DiskHashes();
			SessionState.SetString(P + "guard_memory", JObject.FromObject(memory).ToString(Newtonsoft.Json.Formatting.None));
			SessionState.SetString(P + "guard_disk", JObject.FromObject(disk).ToString(Newtonsoft.Json.Formatting.None));
			Report("guard_baseline_ms", sw.Elapsed.TotalMilliseconds);
			Report("guard_assets", new JArray(memory.Keys.OrderBy(k => k, StringComparer.Ordinal)));
			if (!EnterSandbox(identity))
			{
				Finish(SimulationBatch.ExitGuard);
				return false;
			}
			WriteSandboxProof();
			return true;
		}

		// <out_dir>/sandbox.json: the sandbox section as soon as the rename is proven, with this editor's pid.
		// The launcher reads it to attribute real-save writes to the user's own editor while this one is
		// still running (serve mode); run mode also gets it, next to editor_report.json.
		private static void WriteSandboxProof()
		{
			string outDir = SessionState.GetString(P + "out_dir", "");
			if (string.IsNullOrEmpty(outDir)) return;
			Directory.CreateDirectory(outDir);
			var proof = new JObject
			{
				["schema"] = "tufu.sim.sandbox/1",
				["entry"] = SessionState.GetString(P + "entry", ""),
				["pid"] = Process.GetCurrentProcess().Id,
				["sandbox"] = LoadReport()["sandbox"],
			};
			SimulationJson.WriteText(Path.Combine(outDir, "sandbox.json"), Indented(proof));
		}

		private static string PlistPath(string company, string product) =>
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences", "unity." + company + "." + product + ".plist");

		private static bool EnterSandbox(string identity)
		{
			string company = PlayerSettings.companyName, product = PlayerSettings.productName;
			string realData = Application.persistentDataPath;
			bool wasDirty = EditorUtility.IsDirty(PlayerSettingsObject());
			SessionState.SetBool(P + "was_dirty", wasDirty);
			SessionState.SetString(P + "company", company);
			SessionState.SetString(P + "product", product);
			SessionState.SetBool(P + "renamed", true);
			PlayerSettings.companyName = SimulationRunRequest.SandboxCompany;
			PlayerSettings.productName = identity;
			string sandboxData = Application.persistentDataPath;
			var sandbox = new JObject
			{
				["real_company"] = company,
				["real_product"] = product,
				["real_persistent_data_path"] = realData,
				["real_plist"] = PlistPath(company, product),
				["sandbox_company"] = SimulationRunRequest.SandboxCompany,
				["sandbox_product"] = identity,
				["sandbox_persistent_data_path"] = sandboxData,
				["sandbox_plist"] = PlistPath(SimulationRunRequest.SandboxCompany, identity),
				["player_settings_dirty_before"] = wasDirty,
			};
			Report("sandbox", sandbox);
			if (sandboxData == realData || !sandboxData.Contains(SimulationRunRequest.SandboxCompany) || !sandboxData.Contains(identity))
			{
				Debug.LogError(LogTag + "persistentDataPath did not follow the rename: " + sandboxData);
				return false;
			}
			// PlayerPrefs probe, read back through cfprefsd: it must land in the sandbox domain only.
			string stamp = Guid.NewGuid().ToString("N");
			PlayerPrefs.SetString(ProbeKey, stamp);
			PlayerPrefs.Save();
			string sandboxDomain = "unity." + SimulationRunRequest.SandboxCompany + "." + identity, realDomain = "unity." + company + "." + product;
			bool inSandbox = false, inReal = false;
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed.TotalSeconds < 5 && !inSandbox && !inReal)
			{
				inSandbox = DefaultsRead(sandboxDomain, ProbeKey) == stamp;
				inReal = DefaultsRead(realDomain, ProbeKey) == stamp;
				if (!inSandbox && !inReal) System.Threading.Thread.Sleep(100);
			}
			PlayerPrefs.DeleteKey(ProbeKey);
			PlayerPrefs.Save();
			sandbox["prefs_probe_in_sandbox_domain"] = inSandbox;
			sandbox["prefs_probe_in_real_domain"] = inReal;
			sandbox["prefs_probe_wait_ms"] = sw.ElapsedMilliseconds;
			Report("sandbox", sandbox);
			if (!inSandbox || inReal)
			{
				Debug.LogError(LogTag + "PlayerPrefs did not follow the rename (sandbox " + inSandbox + ", real " + inReal + ")");
				return false;
			}
			Debug.Log(LogTag + "sandbox persistentDataPath " + sandboxData + ", prefs domain " + sandboxDomain);
			return true;
		}

		private static void ClearSandboxData()
		{
			string data = Application.persistentDataPath;
			if (!data.Contains(SimulationRunRequest.SandboxCompany) || PlayerSettings.companyName != SimulationRunRequest.SandboxCompany)
				throw new InvalidOperationException("not in the sandbox: " + data);
			PlayerPrefs.DeleteAll();
			PlayerPrefs.Save();
			if (Directory.Exists(data)) Directory.Delete(data, true);
		}

		private static void SeedPrefs(SimulationRunRequest request)
		{
			foreach (var seed in request.Sandbox.PrefsSeed.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				switch (seed.Value.Type)
				{
					case JTokenType.Integer: PlayerPrefs.SetInt(seed.Key, seed.Value.Value<int>()); break;
					case JTokenType.Float: PlayerPrefs.SetFloat(seed.Key, seed.Value.Value<float>()); break;
					default: PlayerPrefs.SetString(seed.Key, seed.Value.Value<string>()); break;
				}
			}
			PlayerPrefs.Save();
		}

		private static void LeaveSandbox()
		{
			if (!SessionState.GetBool(P + "renamed", false)) return;
			// Flush writes made without Save() during Play (e.g. Boot's game_loaded_once) into the sandbox
			// domain before the names change back.
			PlayerPrefs.Save();
			PlayerSettings.companyName = SessionState.GetString(P + "company", PlayerSettings.companyName);
			PlayerSettings.productName = SessionState.GetString(P + "product", PlayerSettings.productName);
			SessionState.EraseBool(P + "renamed");
			if (!SessionState.GetBool(P + "was_dirty", true)) EditorUtility.ClearDirty(PlayerSettingsObject());
			Report("restored", new JObject
			{
				["company"] = PlayerSettings.companyName,
				["product"] = PlayerSettings.productName,
				["persistent_data_path"] = Application.persistentDataPath,
				["player_settings_dirty"] = EditorUtility.IsDirty(PlayerSettingsObject()),
			});
		}

		private static UnityEngine.Object PlayerSettingsObject() =>
			AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();

		private static string DefaultsRead(string domain, string key)
		{
			var info = new ProcessStartInfo("/usr/bin/defaults", "read \"" + domain + "\" " + key)
			{
				UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
			};
			using (Process p = Process.Start(info))
			{
				string output = p.StandardOutput.ReadToEnd().Trim();
				p.StandardError.ReadToEnd();
				p.WaitForExit(5000);
				return p.ExitCode == 0 ? output : null;
			}
		}

		// ---- jobs ----

		private static SimulationRunRequest CurrentRequest() => SimulationRunRequest.Load(SessionState.GetString(P + "request_path", ""));

		private static void StartJob()
		{
			SimulationRunRequest request = CurrentRequest();
			int index = SessionState.GetInt(P + "job_index", 0);
			SimulationRunRequest.Job job = request.Jobs[index];
			if (job.Probes != null) SimulationProbes.RunEdit(request, job); // C2: edit-mode probes before Play
			ClearSandboxData();
			SeedPrefs(request);
			var marker = new SimulationSandbox.ActiveRequest
			{
				Pid = Process.GetCurrentProcess().Id,
				RequestPath = SessionState.GetString(P + "request_path", ""),
				JobIndex = index,
				Entry = SessionState.GetString(P + "entry", "run"),
			};
			SimulationJson.WriteFile(Path.Combine(SimulationSandbox.TempDir, SimulationSandbox.ActiveRequestFile), marker);
			var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
			if (boot == null) throw new InvalidOperationException("Boot scene not found");
			EditorSceneManager.playModeStartScene = boot;
			SessionState.SetString(P + "stage", "play");
			SessionState.SetInt(P + "job_exit", -1);
			SessionState.SetBool(P + "exit_requested", false);
			SessionState.SetFloat(P + "job_limit", (float)(request.Unity.ReadyTimeoutSeconds + (job.Stop.MaxWallSeconds ?? 3600) + JobSlackSeconds));
			SessionState.SetString(P + "job_timings", new JObject { ["t_play_requested"] = Seconds() }.ToString(Newtonsoft.Json.Formatting.None));
			Debug.Log(LogTag + "job " + job.JobId + " (" + (index + 1) + "/" + request.Jobs.Count + "): entering Play from Boot");
			EditorApplication.isPlaying = true;
		}

		private static void Hook()
		{
			if (hooked) return;
			hooked = true;
			EditorApplication.update += Update;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void Update()
		{
			if (!IsActive()) return;
			try
			{
				editTicks++;
				string stage = SessionState.GetString(P + "stage", "");
				if (stage == "play") UpdatePlay();
				else if (stage == "idle") UpdateIdle();
				if (!EditorApplication.isPlaying || SimulationHarness.Phase == SimulationHarness.HarnessPhase.None) EditBeat(stage);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Finish(SimulationBatch.ExitException);
			}
		}

		private static void UpdatePlay()
		{
			if (!EditorApplication.isPlaying || SessionState.GetBool(P + "exit_requested", false)) return;
			JObject t = JobTimings();
			double sincePlay = Seconds() - (double)t["t_play_requested"];
			int code = -1;
			if (SimulationHarness.Finished) code = SimulationHarness.ExitCode;
			else if (SimulationHarness.Phase == SimulationHarness.HarnessPhase.None && sincePlay > HarnessStartTimeoutSeconds)
			{
				Debug.LogError(LogTag + "the runtime harness did not start");
				code = SimulationBatch.ExitNotReady;
			}
			else if (sincePlay > SessionState.GetFloat(P + "job_limit", 7200))
			{
				Debug.LogError(LogTag + "job exceeded " + SessionState.GetFloat(P + "job_limit", 7200) + " s");
				code = SimulationBatch.ExitTimeout;
			}
			if (code < 0) return;
			t["t_harness_done"] = Seconds();
			SaveJobTimings(t);
			SessionState.SetInt(P + "job_exit", code);
			SessionState.SetBool(P + "exit_requested", true);
			EditorApplication.isPlaying = false;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (!IsActive() || SessionState.GetString(P + "stage", "") != "play") return;
			JObject t = JobTimings();
			t["t_" + state] = Seconds();
			SaveJobTimings(t);
			if (state != PlayModeStateChange.EnteredEditMode) return;
			try
			{
				AfterJob();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Finish(SimulationBatch.ExitException);
			}
		}

		private static void AfterJob()
		{
			SimulationSandbox.Deactivate();
			DeleteMarker();
			PlayerPrefs.Save();
			SimulationRunRequest request = CurrentRequest();
			int index = SessionState.GetInt(P + "job_index", 0);
			SimulationRunRequest.Job job = request.Jobs[index];
			int code = SessionState.GetInt(P + "job_exit", -1);
			if (code < 0) code = SimulationHarness.Finished ? SimulationHarness.ExitCode : SimulationBatch.ExitException;

			bool reimport = job.Test == null || !job.Test.SkipReimport;
			JObject guard = SimulationAssetGuard.CheckAndRestore(GuardBaseline("guard_memory"), GuardBaseline("guard_disk"), reimport);
			bool guardOk = (bool)guard["ok"];
			string dir = request.JobDir(job);
			Directory.CreateDirectory(dir);
			JObject guardFile = new JObject { ["job_id"] = job.JobId, ["after_job"] = guard };
			SimulationJson.WriteText(Path.Combine(dir, "asset_guard.json"), Indented(guardFile));
			if (!guardOk)
			{
				Debug.LogError(LogTag + "asset guard failed after job " + job.JobId + ": " + guard.ToString(Newtonsoft.Json.Formatting.None));
				if (code == SimulationBatch.ExitOk) code = SimulationBatch.ExitGuard;
			}

			JObject t = JobTimings();
			t["t_after_guard"] = Seconds();
			MergeTimings(dir, t);
			var results = JArray.Parse(SessionState.GetString(P + "job_results", "[]"));
			results.Add(new JObject { ["job_id"] = job.JobId, ["exit_code"] = code, ["stop_reason"] = SimulationHarness.StopReason, ["error"] = SimulationHarness.Error, ["asset_guard_ok"] = guardOk });
			SessionState.SetString(P + "job_results", results.ToString(Newtonsoft.Json.Formatting.None));
			Debug.Log(LogTag + "job " + job.JobId + " finished: exit " + code + ", guard " + (guardOk ? "ok" : "FAILED"));

			if (code == SimulationBatch.ExitOk && index + 1 < request.Jobs.Count)
			{
				SessionState.SetInt(P + "job_index", index + 1);
				EditorApplication.delayCall += () =>
				{
					try
					{
						StartJob();
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						Finish(SimulationBatch.ExitException);
					}
				};
				return;
			}
			FinishRequest(code, !guardOk);
		}

		private static void FinishRequest(int code, bool guardFailed)
		{
			if (SessionState.GetString(P + "mode", "run") == "run")
			{
				Finish(code);
				return;
			}
			string running = SessionState.GetString(P + "request_path", "");
			string queue = SessionState.GetString(P + "queue_dir", "");
			string name = Path.GetFileName(running);
			var result = new JObject
			{
				["request"] = name,
				["exit_code"] = code,
				["jobs"] = JArray.Parse(SessionState.GetString(P + "job_results", "[]")),
			};
			SimulationJson.WriteText(Path.Combine(queue, "done", Path.GetFileNameWithoutExtension(name) + ".result.json"), Indented(result));
			string done = Path.Combine(queue, "done", name);
			if (File.Exists(done)) File.Delete(done);
			if (File.Exists(running)) File.Move(running, done);
			SessionState.SetString(P + "stage", "idle");
			SessionState.SetString(P + "last_activity", Now());
			JObject report = LoadReport();
			var served = report["served"] as JArray ?? new JArray();
			served.Add(result);
			report["served"] = served;
			SaveReport(report);
			if (guardFailed) Finish(SimulationBatch.ExitGuard);
		}

		// ---- serve ----

		private static void UpdateIdle()
		{
			string queue = SessionState.GetString(P + "queue_dir", "");
			double now = Seconds();
			if (now - lastInboxPoll < 0.5) return;
			lastInboxPoll = now;
			if (File.Exists(Path.Combine(queue, "stop")))
			{
				Debug.Log(LogTag + "stop file found");
				Finish(SimulationBatch.ExitOk);
				return;
			}
			double idle = (DateTime.UtcNow - DateTime.Parse(SessionState.GetString(P + "last_activity", Now()), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)).TotalSeconds;
			if (idle > SessionState.GetFloat(P + "idle_timeout", 1800))
			{
				Debug.Log(LogTag + "idle timeout");
				Finish(SimulationBatch.ExitOk);
				return;
			}
			string next = Directory.GetFiles(Path.Combine(queue, "inbox"), "*.json").OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
			if (next == null) return;
			string running = Path.Combine(queue, "running", Path.GetFileName(next));
			if (File.Exists(running)) File.Delete(running);
			File.Move(next, running);
			SessionState.SetString(P + "request_path", running);
			SessionState.SetString(P + "job_results", "[]");
			SessionState.SetString(P + "last_activity", Now());
			string error;
			SimulationRunRequest request = null;
			try
			{
				request = SimulationRunRequest.Load(running);
				error = request.Validate();
			}
			catch (Exception e)
			{
				error = e.GetType().Name + ": " + e.Message;
			}
			if (error == null && (request.CodeRev ?? "") != SessionState.GetString(P + "code_rev", ""))
				error = "code_rev " + request.CodeRev + " differs from the resident editor's " + SessionState.GetString(P + "code_rev", "");
			if (error == null && request.Sandbox.Identity != SessionState.GetString(P + "identity", ""))
				error = "sandbox.identity differs from the resident editor's";
			if (error != null)
			{
				Debug.LogError(LogTag + "rejected " + Path.GetFileName(next) + ": " + error);
				var results = new JArray { new JObject { ["error"] = error } };
				SessionState.SetString(P + "job_results", results.ToString(Newtonsoft.Json.Formatting.None));
				FinishRequest(SimulationBatch.ExitBadRequest, false);
				return;
			}
			Directory.CreateDirectory(request.OutDir);
			SessionState.SetInt(P + "job_index", 0);
			StartJob();
		}

		// ---- finish ----

		private static void Finish(int code)
		{
			if (SessionState.GetBool(P + "finishing", false)) return;
			SessionState.SetBool(P + "finishing", true);
			try
			{
				SimulationSandbox.Deactivate();
				DeleteMarker();
				LeaveSandbox();
				Report("exit_code", code);
				Report("job_results", JArray.Parse(SessionState.GetString(P + "job_results", "[]")));
				Report("t_exit", Seconds());
				string outDir = SessionState.GetString(P + "out_dir", "");
				if (!string.IsNullOrEmpty(outDir) && Directory.Exists(outDir))
					SimulationJson.WriteText(Path.Combine(outDir, SessionState.GetString(P + "mode", "run") == "serve" ? "serve_report.json" : "editor_report.json"), Indented(LoadReport()));
				SimulationHarness.ConfigureHeartbeat(Path.Combine(SimulationSandbox.TempDir, "heartbeat.json"), 5, null);
				SimulationHarness.WriteHeartbeat("exit", null, "exit:" + code, 0, 0);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				SessionState.EraseBool(P + "active");
				Debug.Log(LogTag + "exit " + code);
				EditorApplication.Exit(code);
			}
		}

		private static void DeleteMarker()
		{
			string marker = Path.Combine(SimulationSandbox.TempDir, SimulationSandbox.ActiveRequestFile);
			if (File.Exists(marker)) File.Delete(marker);
		}

		// ---- state helpers ----

		private static bool IsActive() =>
			SessionState.GetBool(P + "active", false) && SessionState.GetInt(P + "pid", -1) == Process.GetCurrentProcess().Id;

		private static void ResetSession()
		{
			foreach (string key in new[] { "stage", "request_path", "job_results", "queue_dir", "out_dir", "renamed", "finishing", "report" })
			{
				SessionState.EraseString(P + key);
				SessionState.EraseBool(P + key);
			}
			SessionState.SetBool(P + "active", true);
			SessionState.SetInt(P + "pid", Process.GetCurrentProcess().Id);
			SessionState.SetString(P + "t0", Now());
			Directory.CreateDirectory(SimulationSandbox.TempDir);
			SimulationHarness.ConfigureHeartbeat(Path.Combine(SimulationSandbox.TempDir, "heartbeat.json"), 5, null);
		}

		private static JObject NewReport(string entry)
		{
			DateTime start = Process.GetCurrentProcess().StartTime.ToUniversalTime();
			var report = new JObject
			{
				["schema"] = "tufu.sim.editor_report/1",
				["entry"] = entry,
				["pid"] = Process.GetCurrentProcess().Id,
				["unity_version"] = Application.unityVersion,
				["batchmode"] = Application.isBatchMode,
				["graphics_device"] = SystemInfo.graphicsDeviceType.ToString(),
				["E_cold_start_s"] = (DateTime.UtcNow - start).TotalSeconds,
			};
			SaveReport(report);
			return report;
		}

		private static JObject LoadReport() => JObject.Parse(SessionState.GetString(P + "report", "{}"));

		private static void SaveReport(JObject report) => SessionState.SetString(P + "report", report.ToString(Newtonsoft.Json.Formatting.None));

		private static void Report(string key, JToken value)
		{
			JObject report = LoadReport();
			report[key] = value;
			SaveReport(report);
		}

		private static JObject JobTimings() => JObject.Parse(SessionState.GetString(P + "job_timings", "{}"));

		private static void SaveJobTimings(JObject t) => SessionState.SetString(P + "job_timings", t.ToString(Newtonsoft.Json.Formatting.None));

		private static Dictionary<string, string> GuardBaseline(string key) =>
			JObject.Parse(SessionState.GetString(P + key, "{}")).Properties().ToDictionary(p => p.Name, p => (string)p.Value);

		// Adds the editor's view (Play entry, harness done, Play exit, guard) to the harness timings.
		private static void MergeTimings(string dir, JObject t)
		{
			string path = Path.Combine(dir, "timings.json");
			JObject timings = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
			double play = (double)t["t_play_requested"];
			var editor = new JObject();
			foreach (JProperty p in t.Properties()) editor[p.Name.Replace("t_", "") + "_s"] = (double)p.Value - play;
			timings["editor"] = editor;
			if (t["t_EnteredPlayMode"] != null && timings["ready_s"] != null)
				timings["B_play_to_ready_s"] = (double)t["t_EnteredPlayMode"] - play + (double)timings["ready_s"];
			if (t["t_harness_done"] != null && t["t_EnteredEditMode"] != null)
				timings["X_exit_play_s"] = (double)t["t_EnteredEditMode"] - (double)t["t_harness_done"];
			SimulationJson.WriteText(path, Indented(timings));
		}

		private static void EditBeat(string stage)
		{
			double interval = SessionState.GetFloat(P + "beat_interval", 5);
			if (beatClock.IsRunning && beatClock.Elapsed.TotalSeconds < interval) return;
			beatClock.Restart();
			SimulationHarness.ConfigureHeartbeat(Path.Combine(SimulationSandbox.TempDir, "heartbeat.json"), interval, null);
			SimulationHarness.WriteHeartbeat("edit:" + stage, null, "edit:" + stage + ":" + editTicks, 0, 0);
		}

		private static string Indented(JObject value) => value.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n";

		private static string Now() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

		private static double Seconds() =>
			(DateTime.UtcNow - DateTime.Parse(SessionState.GetString(P + "t0", Now()), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)).TotalSeconds;
	}
}
