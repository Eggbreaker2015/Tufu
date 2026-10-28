using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Codebase.ABTests;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Services.Time;
using Codebase.Simulation;
using Codebase.Simulation.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.EditorTools
{
    // Minimal headless entry (stage 5, C0a spike; handed over to C0b).
    //   Unity -batchmode -projectPath <run worktree>/Recovery/StudyProject \
    //     -disable-assembly-updater Unity.Mathematics.dll Unity.Burst.dll -logFile <dir>/unity.log \
    //     -executeMethod Codebase.Simulation.EditorTools.SimulationBatch.Run -tufuSpikeOut <dir>
    // No -quit: the editor enters Play from Boot, waits for game readiness, measures, exits Play and
    // ends with EditorApplication.Exit(code). PlayerSettings company/product names are swapped in memory
    // for the whole run so persistentDataPath and the PlayerPrefs plist point at a sandbox.
    // RenameProbe runs only the edit-mode half of the sandbox check (no Play).
    [InitializeOnLoad]
    public static class SimulationBatch
    {
        public const int ExitOk = 0, ExitException = 1, ExitGuard = 2, ExitTimeout = 3, ExitNotReady = 4, ExitBadRequest = 5;

        private const string Prefix = "TufuSimSpike.";
        private const string LogTag = "[TufuSimSpike] ";
        private const string SandboxCompany = "TufuSimSandbox";
        private const string SandboxProduct = "C0a-spike";
        private const string ProbeKey = "tufu_sandbox_probe";
        private const double StableReadySeconds = 1.0;

        private static readonly StringBuilder report = new StringBuilder();
        private static double readyTimeoutSeconds = 300, hardTimeoutSeconds = 600;
        private static string outDir;
        private static bool updateHooked;
        private static Phase phase;
        private static double readyStreakStart = -1;
        private static int readyStreakSpawners = -1;
        private static int frames;
        private static readonly List<string> errors = new List<string>();
        private static int errorCount, exceptionCount;
        private static ProbeState probe;

        private enum Phase { None, WaitReady, Probing, Exiting, Done }

        static SimulationBatch()
        {
            if (!IsActiveSession()) return;
            outDir = SessionState.GetString(Prefix + "out", null);
            readyTimeoutSeconds = SessionState.GetFloat(Prefix + "readyTimeout", 300f);
            hardTimeoutSeconds = SessionState.GetFloat(Prefix + "hardTimeout", 600f);
            report.Append(SessionState.GetString(Prefix + "report", string.Empty));
            Stamp("t_domain_reloaded");
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
            HookUpdate();
            if (EditorApplication.isPlayingOrWillChangePlaymode) phase = Phase.WaitReady;
            else
            {
                // A reload back in edit mode (not expected): finish with the code requested before it.
                phase = Phase.Exiting;
                int code = SessionState.GetInt(Prefix + "exitCode", ExitException);
                EditorApplication.delayCall += () => Finish(code);
            }
        }

        // -executeMethod entry.
        public static void Run()
        {
            try
            {
                Begin(probeOnly: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish(ExitException);
            }
        }

        // -executeMethod entry: rename experiment in edit mode only.
        public static void RenameProbe()
        {
            try
            {
                Begin(probeOnly: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish(ExitException);
            }
        }

        private static void Begin(bool probeOnly)
        {
            report.Clear();
            DateTime processStart = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            Put("t_process_start", processStart.ToString("o"));
            Stamp("t_run");
            Put("E_cold_start_s", (DateTime.UtcNow - processStart).TotalSeconds);
            Put("unity_version", Application.unityVersion);
            Put("batchmode", Application.isBatchMode);
            Put("graphics_device", SystemInfo.graphicsDeviceType.ToString());
            Put("enter_play_mode_options_enabled", EditorSettings.enterPlayModeOptionsEnabled);
            Put("enter_play_mode_options", EditorSettings.enterPlayModeOptions.ToString());

            string[] args = Environment.GetCommandLineArgs();
            outDir = GetArg(args, "-tufuSpikeOut");
            if (string.IsNullOrEmpty(outDir))
            {
                Debug.LogError(LogTag + "missing -tufuSpikeOut <dir>");
                EditorApplication.Exit(ExitBadRequest);
                return;
            }
            Directory.CreateDirectory(outDir);
            if (double.TryParse(GetArg(args, "-tufuReadyTimeout"), NumberStyles.Float, CultureInfo.InvariantCulture, out double rt)) readyTimeoutSeconds = rt;
            SessionState.SetString(Prefix + "out", outDir);
            SessionState.SetFloat(Prefix + "readyTimeout", (float)readyTimeoutSeconds);
            SessionState.SetFloat(Prefix + "hardTimeout", (float)hardTimeoutSeconds);
            SessionState.SetInt(Prefix + "pid", Process.GetCurrentProcess().Id);

            // Reload Domain and Scene must stay on (project README); options None with the toggle on means both reload.
            bool reloads = !EditorSettings.enterPlayModeOptionsEnabled ||
                           (EditorSettings.enterPlayModeOptions & (EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload)) == 0;
            Put("domain_and_scene_reload_on", reloads);
            if (!reloads)
            {
                Debug.LogError(LogTag + "Enter Play Mode options disable domain or scene reload");
                Finish(ExitGuard);
                return;
            }

            if (!EnterSandbox())
            {
                Finish(ExitGuard);
                return;
            }
            if (probeOnly)
            {
                Finish(ExitOk);
                return;
            }

            ClearSandboxData();
            var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
            if (boot == null)
            {
                Debug.LogError(LogTag + "Boot scene not found");
                Finish(ExitBadRequest);
                return;
            }
            EditorSceneManager.playModeStartScene = boot;
            SessionState.SetBool(Prefix + "active", true);
            Stamp("t_play_requested");
            SessionState.SetString(Prefix + "report", report.ToString());
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            HookUpdate();
            phase = Phase.WaitReady;
            Debug.Log(LogTag + "entering Play mode from Boot");
            EditorApplication.isPlaying = true;
        }

        // ---- sandbox (PlayerSettings rename) ----

        private static string RealPrefsPath(string company, string product) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences", "unity." + company + "." + product + ".plist");

        private static bool EnterSandbox()
        {
            string company = PlayerSettings.companyName, product = PlayerSettings.productName;
            string realData = Application.persistentDataPath;
            Put("real_company", company);
            Put("real_product", product);
            Put("real_persistent_data_path", realData);
            bool wasDirty = EditorUtility.IsDirty(PlayerSettingsObject());
            Put("player_settings_dirty_before", wasDirty);
            SessionState.SetBool(Prefix + "wasDirty", wasDirty);
            SessionState.SetString(Prefix + "company", company);
            SessionState.SetString(Prefix + "product", product);
            SessionState.SetBool(Prefix + "renamed", true);
            PlayerSettings.companyName = SandboxCompany;
            PlayerSettings.productName = SandboxProduct;
            string sandboxData = Application.persistentDataPath;
            Put("sandbox_persistent_data_path_edit", sandboxData);
            Put("player_settings_dirty_after_rename", EditorUtility.IsDirty(PlayerSettingsObject()));
            bool dataOk = sandboxData != realData && sandboxData.Contains(SandboxCompany) && sandboxData.Contains(SandboxProduct);
            Put("sandbox_data_path_ok", dataOk);
            if (!dataOk)
            {
                Debug.LogError(LogTag + "persistentDataPath did not follow the rename: " + sandboxData);
                return false;
            }

            // PlayerPrefs probe: one key, read back from the plist files on disk.
            string realPlist = RealPrefsPath(company, product), sandboxPlist = RealPrefsPath(SandboxCompany, SandboxProduct);
            Put("sandbox_plist", sandboxPlist);
            string stamp = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(ProbeKey, stamp);
            PlayerPrefs.Save();
            bool inSandbox = false, inReal = false;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 5 && !inSandbox)
            {
                inSandbox = FileContains(sandboxPlist, stamp);
                inReal = FileContains(realPlist, stamp);
                if (inReal) break;
                System.Threading.Thread.Sleep(100);
            }
            Put("prefs_probe_in_sandbox_plist", inSandbox);
            Put("prefs_probe_in_real_plist", inReal);
            Put("prefs_probe_wait_ms", sw.ElapsedMilliseconds);
            PlayerPrefs.DeleteKey(ProbeKey);
            PlayerPrefs.Save();
            if (!inSandbox || inReal)
            {
                Debug.LogError(LogTag + "PlayerPrefs did not follow the rename (sandbox " + inSandbox + ", real " + inReal + ")");
                return false;
            }
            return true;
        }

        private static void ClearSandboxData()
        {
            string data = Application.persistentDataPath;
            if (!data.Contains(SandboxCompany) || PlayerSettings.companyName != SandboxCompany) throw new InvalidOperationException("not in sandbox: " + data);
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            if (Directory.Exists(data)) Directory.Delete(data, true);
        }

        private static void LeaveSandbox()
        {
            if (!SessionState.GetBool(Prefix + "renamed", false)) return;
            PlayerSettings.companyName = SessionState.GetString(Prefix + "company", PlayerSettings.companyName);
            PlayerSettings.productName = SessionState.GetString(Prefix + "product", PlayerSettings.productName);
            SessionState.EraseBool(Prefix + "renamed");
            // The names are back to their saved values; keep a later SaveAssets from rewriting ProjectSettings.asset.
            if (!SessionState.GetBool(Prefix + "wasDirty", true)) EditorUtility.ClearDirty(PlayerSettingsObject());
            Put("restored_company", PlayerSettings.companyName);
            Put("restored_product", PlayerSettings.productName);
            Put("restored_persistent_data_path", Application.persistentDataPath);
            Put("player_settings_dirty_after_restore", EditorUtility.IsDirty(PlayerSettingsObject()));
            Put("prefs_probe_visible_after_restore", PlayerPrefs.HasKey(ProbeKey));
        }

        private static UnityEngine.Object PlayerSettingsObject() =>
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();

        private static bool FileContains(string path, string text)
        {
            try
            {
                return File.Exists(path) && Encoding.ASCII.GetString(File.ReadAllBytes(path)).Contains(text);
            }
            catch (IOException)
            {
                return false;
            }
        }

        // ---- Play-mode driving ----

        private static bool IsActiveSession() =>
            SessionState.GetBool(Prefix + "active", false) &&
            SessionState.GetInt(Prefix + "pid", -1) == Process.GetCurrentProcess().Id;

        private static void HookUpdate()
        {
            if (updateHooked) return;
            updateHooked = true;
            EditorApplication.update += Update;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Stamp("t_" + state);
            if (state == PlayModeStateChange.EnteredEditMode && phase == Phase.Exiting) Finish(probe != null && probe.ExitCode != 0 ? probe.ExitCode : ExitOk);
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.StartsWith(LogTag)) return;
            lock (errors)
            {
                if (type == LogType.Exception) exceptionCount++;
                else errorCount++;
                string line = type + ": " + message.Split('\n')[0];
                if (errors.Count < 200 && !errors.Contains(line)) errors.Add(line);
            }
        }

        private static void Update()
        {
            try
            {
                double sinceRequest = Seconds("t_play_requested");
                if (phase != Phase.Done && sinceRequest > hardTimeoutSeconds)
                {
                    Debug.LogError(LogTag + "hard timeout");
                    RequestExit(ExitTimeout);
                    return;
                }
                if (!EditorApplication.isPlaying) return;
                frames++;
                switch (phase)
                {
                    case Phase.WaitReady:
                        PollReady(sinceRequest);
                        break;
                    case Phase.Probing:
                        if (probe.Step()) RequestExit(probe.ExitCode);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                RequestExit(ExitException);
            }
        }

        private static void PollReady(double sinceRequest)
        {
            if (sinceRequest > readyTimeoutSeconds)
            {
                Debug.LogError(LogTag + "game not ready after " + readyTimeoutSeconds + " s");
                Put("not_ready_state", DescribeReadiness());
                RequestExit(ExitNotReady);
                return;
            }
            if (!IsReady(out int spawners))
            {
                readyStreakStart = -1;
                return;
            }
            double now = Seconds("t_play_requested");
            if (readyStreakStart < 0 || spawners != readyStreakSpawners)
            {
                readyStreakStart = now;
                readyStreakSpawners = spawners;
                Stamp("t_ready");
                Put("B_play_to_ready_s", now);
                Put("frames_to_ready", frames);
                return;
            }
            if (now - readyStreakStart < StableReadySeconds) return;
            Debug.Log(LogTag + "ready: " + spawners + " spawners after " + readyStreakStart.ToString("F2", CultureInfo.InvariantCulture) + " s");
            phase = Phase.Probing;
            probe = new ProbeState();
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

        private static void RequestExit(int code)
        {
            if (phase == Phase.Exiting || phase == Phase.Done) return;
            if (probe == null) probe = new ProbeState();
            probe.ExitCode = code;
            SessionState.SetInt(Prefix + "exitCode", code);
            phase = Phase.Exiting;
            Stamp("t_exit_play_requested");
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            else Finish(code);
        }

        private static void Finish(int code)
        {
            if (phase == Phase.Done) return;
            phase = Phase.Done;
            try
            {
                if (report.ToString().Contains("t_exit_play_requested=") && report.ToString().Contains("t_EnteredEditMode="))
                    Put("X_exit_play_s", Seconds("t_exit_play_requested") - Seconds("t_EnteredEditMode"));
                lock (errors)
                {
                    Put("log_errors", errorCount);
                    Put("log_exceptions", exceptionCount);
                    Put("log_error_lines", string.Join(" || ", errors));
                }
                Put("exit_code", code);
                Stamp("t_exit");
                if (!string.IsNullOrEmpty(outDir)) File.WriteAllText(Path.Combine(outDir, "spike_report.txt"), report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Restore the real names in the same call as Exit: nothing in the game can write between the two.
                LeaveSandbox();
                if (!string.IsNullOrEmpty(outDir)) File.WriteAllText(Path.Combine(outDir, "spike_report.txt"), report.ToString());
                SessionState.EraseBool(Prefix + "active");
                SessionState.EraseString(Prefix + "report");
                Debug.Log(LogTag + "exit " + code);
                EditorApplication.Exit(code);
            }
        }

        // ---- measurements after readiness ----

        private sealed class ProbeState
        {
            public int ExitCode;
            private int step;
            private int waitFrames;
            private Stopwatch clock;
            private int frameCount;
            private SimulationConfig config;
            private bool savedRunning;
            private int savedSpeed;

            // Returns true when finished.
            public bool Step()
            {
                switch (step)
                {
                    case 0:
                        Put("sandbox_persistent_data_path_play", Application.persistentDataPath);
                        Put("active_scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                        DumpAb();
                        DumpSpawners("spawners_ready.csv", "ready");
                        clock = Stopwatch.StartNew();
                        frameCount = 0;
                        step = 1;
                        return false;
                    case 1:
                        // Idle frames under the real clock: frame pacing and spawner drift.
                        frameCount++;
                        if (clock.Elapsed.TotalSeconds < 3) return false;
                        Put("idle_frames_3s", frameCount);
                        Put("idle_frame_ms", clock.Elapsed.TotalMilliseconds / frameCount);
                        DumpSpawners("spawners_ready_plus3s.csv", "ready_plus3s");
                        MeasureRecoveredCalls();
                        config = (SimulationConfig)typeof(SimulationSystem).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SimulationSystem.Instance);
                        savedRunning = config.IsRunning;
                        savedSpeed = config.SimulationSpeed;
                        config.SimulationSpeed = 1000000;
                        config.IsRunning = true;
                        clock = Stopwatch.StartNew();
                        frameCount = 0;
                        step = 2;
                        return false;
                    case 2:
                        // Recovered OnUpdate loop with the empty ExecuteTick: pure loop overhead.
                        frameCount++;
                        if (frameCount < 30) return false;
                        Put("empty_tick_loop_frames", frameCount);
                        Put("empty_tick_loop_ticks_per_frame", config.SimulationSpeed);
                        Put("empty_tick_loop_frame_ms", clock.Elapsed.TotalMilliseconds / frameCount);
                        config.IsRunning = savedRunning;
                        config.SimulationSpeed = savedSpeed;
                        RemedyProbe();
                        waitFrames = 0;
                        step = 3;
                        return false;
                    default:
                        return ++waitFrames > 2;
                }
            }

            private static void MeasureRecoveredCalls()
            {
                var system = SimulationSystem.Instance;
                MethodInfo spawnerData = typeof(SimulationSystem).GetMethod("CalculateSpawnerData", BindingFlags.Instance | BindingFlags.NonPublic);
                var sw = Stopwatch.StartNew();
                system.CalculateSimulationData();
                Put("calc_simulation_data_first_ms", sw.Elapsed.TotalMilliseconds);
                const int n = 10;
                sw.Restart();
                for (int i = 0; i < n; i++) system.CalculateSimulationData();
                Put("calc_simulation_data_ms", sw.Elapsed.TotalMilliseconds / n);
                sw.Restart();
                for (int i = 0; i < n; i++) spawnerData.Invoke(system, new object[] { true });
                double gpm = sw.Elapsed.TotalMilliseconds / n;
                sw.Restart();
                for (int i = 0; i < n; i++) spawnerData.Invoke(system, new object[] { false });
                double bot = sw.Elapsed.TotalMilliseconds / n;
                Put("calc_spawner_data_gpm_ms", gpm);
                Put("calc_spawner_data_bot_ms", bot);
                Put("base_tick_estimate_ms", gpm + bot);
                Put("base_ticks_per_1s_frame", Math.Floor(1000.0 / Math.Max(1e-6, gpm + bot)));
            }

            // Q36: which call brings units back under a fixed `now`, before any IsRunning.
            private static void RemedyProbe()
            {
                var model = GameContext.Get<EnemiesModel>();
                var time = GameContext.Get<ITimeService>();
                int now = (int)new DateTimeOffset(time.Now.ToDateTime()).ToUnixTimeSeconds();
                int cooldownVersion = GameContext.Get<ABTestService>().SpawnerCooldownsVersion;
                MetaStat itemStat = GameContext.Get<MetaStatsModel>().Get("items_rarity_charges");
                int itemVersion = itemStat != null ? itemStat.ModifierVersion : 0;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo initNow = typeof(EnemySpawner).GetMethod("InitializeSpawnerNow", flags);
                FieldInfo nextProcess = typeof(EnemySpawner).GetField("nextProcessTime", flags);
                PropertyInfo lastSpawn = typeof(EnemySpawner).GetProperty("LastSpawnTime", flags);
                var rows = new StringBuilder("key,location,is_event,is_vip,next_process_before,last_spawn_before,charges_left,after_process_spawn,after_process_spawn_again,after_initialize_now,after_initialize_now_plus_1d\n");
                int candidates = 0, byProcess = 0, byInit = 0, byInitLater = 0;
                var sw = new Stopwatch();
                foreach (EnemySpawner s in model.Spawners.ToList())
                {
                    if (s.AnyoneAlive || s.Config.IsEvent || s.Config.IsVip) continue;
                    candidates++;
                    int np = (int)nextProcess.GetValue(s), ls = (int)lastSpawn.GetValue(s, null);
                    sw.Start();
                    s.ProcessSpawn(now, cooldownVersion, itemVersion);
                    int a = s.CurrentCount;
                    s.ProcessSpawn(now, cooldownVersion, itemVersion);
                    int a2 = s.CurrentCount;
                    int b = a2;
                    if (a2 == 0)
                    {
                        initNow.Invoke(s, new object[] { now });
                        b = s.CurrentCount;
                    }
                    int c = b;
                    if (b == 0)
                    {
                        initNow.Invoke(s, new object[] { now + 86400 });
                        c = s.CurrentCount;
                    }
                    sw.Stop();
                    if (a > 0) byProcess++;
                    else if (b > 0) byInit++;
                    else if (c > 0) byInitLater++;
                    rows.Append(string.Join(",", Csv(s.Config.Key), s.Config.LocationNumber, s.Config.IsEvent, s.Config.IsVip, np, ls, s.ChargesLeft, a, a2, b, c)).Append('\n');
                }
                File.WriteAllText(Path.Combine(outDir, "spawners_remedy.csv"), rows.ToString());
                Put("remedy_now_unix", now);
                Put("remedy_candidates", candidates);
                Put("remedy_by_process_spawn", byProcess);
                Put("remedy_by_initialize_now", byInit);
                Put("remedy_only_with_later_clock", byInitLater);
                Put("remedy_ms", sw.Elapsed.TotalMilliseconds);
                DumpSpawners("spawners_after_remedy.csv", "after_remedy");
            }

            private static void DumpAb()
            {
                if (!GameContext.Has<ABTestModel>())
                {
                    Put("ab_model", "missing");
                    return;
                }
                var model = GameContext.Get<ABTestModel>();
                var parts = new List<string>();
                foreach (FieldInfo field in typeof(ABKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    string key = (string)field.GetRawConstantValue();
                    parts.Add(key + "=" + (model.GetVariant(key) ?? "null") + "/" + (model.GetVariantList(key) ?? "null"));
                }
                Put("ab_tests", string.Join(";", parts));
            }

            private static void DumpSpawners(string file, string tag)
            {
                var model = GameContext.Get<EnemiesModel>();
                var rows = new StringBuilder("index,key,location,is_event,is_vip,is_dummy,is_static,is_elite,item_reward,behaviour_enabled,is_enabled,anyone_alive,count,max_count,bootstrap_complete,charges_left,charges_total,base_cooldown,cooldown,go_active\n");
                int ok = 0, i = 0;
                var notOk = new List<string>();
                foreach (EnemySpawner s in model.Spawners)
                {
                    EnemySpawnerConfig c = s.Config;
                    bool good = s.AnyoneAlive && s.IsEnabled;
                    if (good) ok++;
                    else notOk.Add(c.Key + (c.IsEvent ? "(event)" : c.IsVip ? "(vip)" : c.IsDummy ? "(dummy)" : s.HasItemReward ? "(item)" : ""));
                    rows.Append(string.Join(",", i++, Csv(c.Key), c.LocationNumber, c.IsEvent, c.IsVip, c.IsDummy, c.IsStatic, c.IsEliteBoss, s.HasItemReward,
                        s.enabled, s.IsEnabled, s.AnyoneAlive, s.CurrentCount, s.MaxCount, s.IsBootstrapComplete, s.ChargesLeft, s.ChargesTotal,
                        s.BaseCooldown.ToString("R", CultureInfo.InvariantCulture), s.Cooldown.ToString("R", CultureInfo.InvariantCulture), s.gameObject.activeInHierarchy)).Append('\n');
                }
                File.WriteAllText(Path.Combine(outDir, file), rows.ToString());
                int sceneSpawners = Resources.FindObjectsOfTypeAll<EnemySpawner>().Count(s => s.gameObject.scene.IsValid());
                Put(tag + "_registered", model.Spawners.Count);
                Put(tag + "_scene_spawner_components", sceneSpawners);
                Put(tag + "_alive_and_enabled", ok);
                Put(tag + "_not_alive_or_disabled", string.Join(" ", notOk));
                Put(tag + "_enemies", model.Enemies.Count);
            }
        }

        // ---- helpers ----

        private static string Csv(string s) => s == null ? "" : s.Contains(",") || s.Contains("\"") ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

        private static string GetArg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static void Put(string key, object value)
        {
            string text = value is double d ? d.ToString("0.###", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
            report.Append(key).Append('=').Append(text).Append('\n');
            Debug.Log(LogTag + key + "=" + text);
        }

        private static void Stamp(string key) => Put(key, DateTime.UtcNow.ToString("o"));

        // Seconds from a stamped time to now; stamps survive the domain reload through the report text.
        private static double Seconds(string key)
        {
            string text = report.ToString();
            int at = text.LastIndexOf("\n" + key + "=", StringComparison.Ordinal);
            if (at < 0 && text.StartsWith(key + "=")) at = -1;
            else if (at < 0) return 0;
            int start = at + key.Length + 2, end = text.IndexOf('\n', start);
            DateTime t = DateTime.Parse(text.Substring(start, end - start), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return (DateTime.UtcNow - t).TotalSeconds;
        }
    }
}
