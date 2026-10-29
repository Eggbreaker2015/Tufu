using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Codebase.Enemy;
using Codebase.Simulation.Harness;
using Codebase.Unit;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.EditorTools
{
	// Bake plan and guarded write-back (design 1.5 C# rule 6, 7.3 C8, Q13), three batch entries that
	// tufu_balance.unity.bake_report starts in a fresh editor on the run worktree:
	//   Plan     -tufuSimBakeReport <job>/bake_report.json -tufuSimBakePlan <plan.json> -tufuSimBakeOut <dir>
	//            opens Main.unity, pairs every scene spawner with its report row and writes the plan: old -> new
	//            Health/Damage per spawner and the skip reasons. Saves nothing.
	//   Apply    -tufuSimBakePlan <plan.json> -tufuSimBakeToken <sha256 of the plan file> -tufuSimBakeOut <dir>
	//            -tufuSimBakeCaller cli
	//            only in a fresh batch editor that never entered Play, in edit mode, with the scene clean and
	//            tracked in git and equal to the planned file, the token matching and no refresh/CI marker; every
	//            planned old value must still be in the scene. Writes through SerializedObject (one Undo group),
	//            copies the scene file to <dir>/backup before saving it, writes <dir>/bake_apply_manifest.json.
	//   SelfTest -tufuSimBakeOut <dir>: the plan and write-back logic on a scene made with
	//            EditorSceneManager.NewScene that is never saved, Undo/Redo, and every refusal of the guard.
	// Only Health/Damage of ordinary spawners are written; SpawnerBakeInfos is left alone (Q13).
	// [InitializeOnLoad]: the Play hook is registered at every domain load, not first use, so a Play entry before
	// any call into this class is still recorded (same guard as SimulationParamsImporter).
	[InitializeOnLoad]
	public static class SimulationBakeTool
	{
		public const string PlanSchema = "tufu.sim.bake-plan/1";
		public const string PlanReportSchema = "tufu.sim.bake-plan-report/1";
		public const string ApplySchema = "tufu.sim.bake-apply/1";
		public const string SelfTestSchema = "tufu.sim.bake-selftest/1";
		public const string MainScenePath = "Assets/Scenes/Main.unity";
		public const string ApplyManifestFile = "bake_apply_manifest.json";
		public const string CallerCli = "cli";
		public const string ActionWrite = "write", ActionUnchanged = "unchanged", ActionSkip = "skip";
		// Skip reasons, in the order they are listed per row.
		public static readonly string[] Reasons =
		{
			"event", "vip", "boss_tower", "dummy", "hand_balance", "non_finite", "not_in_report", "report_error", "no_stat", "duplicate_id",
		};
		// Any of these set in the editor's environment marks a refresh or CI call.
		public static readonly string[] RefusedEnvironment = { "CI", "TUFU_REFRESH", "GITHUB_ACTIONS" };

		private const string EnteredPlayKey = "TufuSimBake.entered_play";
		private const string LogTag = "[TufuSim] ";
		private static readonly string[] Fields = { "health", "damage" };

		// The stack of the static constructor: [InitializeOnLoad] runs it from ProcessInitializeOnLoadAttributes.
		private static readonly string RegisteredFrom;

		static SimulationBakeTool()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			RegisteredFrom = new StackTrace().ToString();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode) SessionState.SetBool(EnteredPlayKey, true);
		}

		// ---- guard ----

		// Null when this editor process may write a scene, else why not.
		public static string ProcessRefusal()
		{
			if (SessionState.GetBool(EnteredPlayKey, false)) return "refused: this editor process has entered Play mode";
			if (EditorApplication.isPlayingOrWillChangePlaymode) return "refused: the editor is in or entering Play mode";
			if (!Application.isBatchMode) return "refused: the bake write-back runs only in a batch-mode editor started for it";
			return null;
		}

		public static string CallerRefusal(string caller, Func<string, string> environment)
		{
			foreach (string name in RefusedEnvironment)
				if (!string.IsNullOrEmpty(environment(name))) return "refused: " + name + " is set (refresh or CI call)";
			if (caller != CallerCli) return "refused: caller " + (caller ?? "(none)") + "; only an explicit bake --apply writes";
			return null;
		}

		public static string TokenRefusal(byte[] plan, string token)
		{
			string sha = Sha256Hex(plan);
			if (string.IsNullOrEmpty(token) || !string.Equals(sha, token.Trim(), StringComparison.Ordinal))
				return "refused: token " + (token ?? "(none)") + " is not the plan's sha256 " + sha;
			return null;
		}

		// The scene must be tracked and unmodified in git (worktree and index). repoDir: the project by default.
		public static string GitRefusal(string assetPath, string repoDir = null)
		{
			string project = repoDir ?? ProjectDir();
			(int code, string output, string error) = Git(project, "status --porcelain -uall -- \"" + assetPath + "\"");
			if (code != 0) return "refused: git status failed for " + assetPath + ": " + error.Trim();
			if (output.Trim().Length > 0) return "refused: " + assetPath + " is not clean in git: " + output.Trim();
			(code, _, _) = Git(project, "ls-files --error-unmatch -- \"" + assetPath + "\"");
			if (code != 0) return "refused: " + assetPath + " is not tracked in git";
			return null;
		}

		// ---- plan ----

		public static JObject BuildPlan(IList<EnemySpawner> spawners, JObject report, JObject header)
		{
			var reportRows = new Dictionary<string, JObject>(StringComparer.Ordinal);
			var duplicateReportIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (JObject r in (JArray)report["spawners"])
			{
				string id = (string)r["id"];
				if (reportRows.ContainsKey(id)) duplicateReportIds.Add(id);
				else reportRows[id] = r;
			}
			var sceneIds = spawners.GroupBy(SimulationRuntimeProbes.SpawnerId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
			var rows = new JArray();
			var byReason = new JObject();
			foreach (string reason in Reasons) byReason[reason] = 0;
			int write = 0, unchanged = 0, skip = 0;
			for (int i = 0; i < spawners.Count; i++)
			{
				EnemySpawner spawner = spawners[i];
				EnemySpawnerConfig c = spawner.Config;
				string id = SimulationRuntimeProbes.SpawnerId(spawner);
				var reasons = new List<string>();
				if (c.IsEvent) reasons.Add("event");
				if (c.IsVip) reasons.Add("vip");
				if (spawner.name.StartsWith("BossTower", StringComparison.Ordinal)) reasons.Add("boss_tower");
				if (c.IsDummy) reasons.Add("dummy");
				if (c.HandBalance) reasons.Add("hand_balance");
				double? newHealth = null, newDamage = null;
				reportRows.TryGetValue(id, out JObject reportRow);
				if (reportRow == null) reasons.Add("not_in_report");
				else if (reportRow["report"] == null || reportRow["report"].Type != JTokenType.Object) reasons.Add("report_error");
				else
				{
					newHealth = SimulationRuntimeProbes.Doubles(reportRow["report"]["NewHealth"])[0];
					newDamage = SimulationRuntimeProbes.Doubles(reportRow["report"]["NewDamage"])[0];
					if (!Finite(newHealth.Value) || !Finite(newDamage.Value)) reasons.Add("non_finite");
				}
				Stat health = c.Stats != null ? c.Stats.FirstOrDefault(s => s.Type == StatType.Health) : null;
				Stat damage = c.Stats != null ? c.Stats.FirstOrDefault(s => s.Type == StatType.Damage) : null;
				if (health == null || damage == null) reasons.Add("no_stat");
				if (sceneIds[id] > 1 || duplicateReportIds.Contains(id)) reasons.Add("duplicate_id");
				reasons = Reasons.Where(reasons.Contains).ToList();

				string action;
				if (reasons.Count > 0) action = ActionSkip;
				else if (Bits(health.BaseValue) == Bits(newHealth.Value) && Bits(damage.BaseValue) == Bits(newDamage.Value)) action = ActionUnchanged;
				else action = ActionWrite;
				if (action == ActionWrite) write++;
				else if (action == ActionUnchanged) unchanged++;
				else skip++;
				foreach (string reason in reasons) byReason[reason] = (int)byReason[reason] + 1;

				var row = new JObject
				{
					["index"] = i,
					["id"] = id,
					["name"] = spawner.name,
					["key"] = c.Key,
					["location"] = c.LocationNumber,
					["is_days"] = c.IsDays,
					["action"] = action,
					["reasons"] = new JArray(reasons.Cast<object>().ToArray()),
				};
				AddValue(row, "old_health", health?.BaseValue);
				AddValue(row, "new_health", newHealth);
				AddValue(row, "old_damage", damage?.BaseValue);
				AddValue(row, "new_damage", newDamage);
				rows.Add(row);
			}
			var plan = new JObject();
			foreach (var kv in header) plan[kv.Key] = kv.Value;
			plan["schema"] = PlanSchema;
			plan["fields"] = new JArray("Health", "Damage");
			plan["counts"] = new JObject
			{
				["spawners"] = spawners.Count,
				["write"] = write,
				["unchanged"] = unchanged,
				["skip"] = skip,
				["by_reason"] = byReason,
				["report_rows"] = ((JArray)report["spawners"]).Count,
			};
			plan["unmatched_report_rows"] = new JArray(reportRows.Keys.Where(k => !sceneIds.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).Cast<object>().ToArray());
			plan["rows"] = rows;
			return plan;
		}

		private static void AddValue(JObject row, string name, double? value)
		{
			if (value.HasValue)
			{
				row[name] = new JRaw(SimulationJson.JsonDouble(value.Value));
				row[name + "_bits"] = SimulationRuntimeProbes.D(value.Value);
			}
			else
			{
				row[name] = null;
				row[name + "_bits"] = null;
			}
		}

		public static string PlanText(JObject plan) => plan.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n";

		// ---- write-back ----

		public sealed class ApplyResult
		{
			public string Refusal;
			public readonly JArray Written = new JArray();
			public int Unchanged, Skipped;
		}

		// Checks every planned write against the scene first (spawner found once, both stats present, current
		// values equal to the planned old ones); on any mismatch nothing is written. Then writes all of them
		// through SerializedObject in one Undo group and reads them back.
		public static ApplyResult ApplyPlan(IList<EnemySpawner> spawners, JObject plan)
		{
			var result = new ApplyResult();
			var byId = spawners.GroupBy(SimulationRuntimeProbes.SpawnerId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
			var writes = new List<(JObject row, EnemySpawner spawner)>();
			foreach (JObject row in (JArray)plan["rows"])
			{
				string action = (string)row["action"];
				if (action == ActionUnchanged) result.Unchanged++;
				if (action != ActionWrite)
				{
					if (action == ActionSkip) result.Skipped++;
					continue;
				}
				string id = (string)row["id"];
				if (!byId.TryGetValue(id, out List<EnemySpawner> found) || found.Count != 1)
				{
					result.Refusal = "refused: stale plan: spawner " + id + " found " + (found?.Count ?? 0) + " times in the scene";
					return result;
				}
				var so = new SerializedObject(found[0]);
				foreach (string field in Fields)
				{
					SerializedProperty p = StatProperty(so, field == "health" ? StatType.Health : StatType.Damage);
					if (p == null)
					{
						result.Refusal = "refused: stale plan: spawner " + id + " has no " + field + " stat";
						return result;
					}
					if (SimulationRuntimeProbes.D(p.doubleValue) != (string)row["old_" + field + "_bits"])
					{
						result.Refusal = "refused: stale plan: spawner " + id + " " + field + " is " + SimulationJson.FormatDouble(p.doubleValue) + ", the plan says " + row["old_" + field];
						return result;
					}
				}
				writes.Add((row, found[0]));
			}
			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName("Tufu bake write-back");
			int group = Undo.GetCurrentGroup();
			foreach (var (row, spawner) in writes)
			{
				var so = new SerializedObject(spawner);
				foreach (string field in Fields)
					StatProperty(so, field == "health" ? StatType.Health : StatType.Damage).doubleValue = SimulationRuntimeProbes.Doubles(row["new_" + field + "_bits"])[0];
				so.ApplyModifiedProperties();
			}
			Undo.CollapseUndoOperations(group);
			foreach (var (row, spawner) in writes)
			{
				Stat health = spawner.Config.Stats.First(s => s.Type == StatType.Health);
				Stat damage = spawner.Config.Stats.First(s => s.Type == StatType.Damage);
				bool ok = SimulationRuntimeProbes.D(health.BaseValue) == (string)row["new_health_bits"] && SimulationRuntimeProbes.D(damage.BaseValue) == (string)row["new_damage_bits"];
				result.Written.Add(new JObject
				{
					["id"] = row["id"],
					["old_health"] = row["old_health"],
					["new_health"] = row["new_health"],
					["old_damage"] = row["old_damage"],
					["new_damage"] = row["new_damage"],
					["read_back"] = ok,
				});
				if (!ok && result.Refusal == null) result.Refusal = "write-back of " + row["id"] + " did not read back";
			}
			return result;
		}

		// "config.Stats.Array.data[i].BaseValue" of the first stat of that type (BuildSpawnerBakeReport's Stats.First).
		public static SerializedProperty StatProperty(SerializedObject so, StatType type)
		{
			SerializedProperty stats = so.FindProperty("config.Stats");
			if (stats == null || !stats.isArray) return null;
			for (int i = 0; i < stats.arraySize; i++)
			{
				SerializedProperty element = stats.GetArrayElementAtIndex(i);
				if (element.FindPropertyRelative("Type").intValue == (int)type) return element.FindPropertyRelative("BaseValue");
			}
			return null;
		}

		// ---- entries ----

		public static void Plan()
		{
			string outDir = SimulationBatch.GetArg("-tufuSimBakeOut");
			var report = new JObject { ["schema"] = PlanReportSchema, ["ok"] = false, ["pid"] = Process.GetCurrentProcess().Id, ["unity_version"] = Application.unityVersion };
			Finish(report, outDir, "bake_plan_report.json", () =>
			{
				report["entered_play"] = SessionState.GetBool(EnteredPlayKey, false);
				string reportPath = SimulationBatch.GetArg("-tufuSimBakeReport");
				string planPath = SimulationBatch.GetArg("-tufuSimBakePlan");
				string scenePath = SimulationBatch.GetArg("-tufuSimBakeScene") ?? MainScenePath;
				if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath) || string.IsNullOrEmpty(planPath))
				{
					report["error"] = "missing -tufuSimBakeReport <bake_report.json> or -tufuSimBakePlan <plan.json>";
					return SimulationBatch.ExitBadRequest;
				}
				byte[] reportBytes = File.ReadAllBytes(reportPath);
				JObject bakeReport = JObject.Parse(Encoding.UTF8.GetString(reportBytes));
				if ((string)bakeReport["schema"] != SimulationBakeExport.Schema)
				{
					report["error"] = "report schema must be " + SimulationBakeExport.Schema;
					return SimulationBatch.ExitBadRequest;
				}
				string sceneFile = FullPath(scenePath);
				report["scene_sha256_before"] = Sha256File(sceneFile);
				var sw = Stopwatch.StartNew();
				Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
				report["open_scene_ms"] = sw.Elapsed.TotalMilliseconds;
				List<EnemySpawner> spawners = SimulationBakeExport.SpawnersOf(scene);
				var header = new JObject
				{
					["scene"] = scenePath,
					["scene_sha256"] = Sha256File(sceneFile),
					["code_rev"] = Git(ProjectDir(), "rev-parse HEAD").Item2.Trim(),
					["report"] = new JObject
					{
						["file"] = Path.GetFileName(reportPath),
						["sha256"] = Sha256Hex(reportBytes),
						["snapshots"] = bakeReport["snapshots"],
					},
				};
				JObject plan = BuildPlan(spawners, bakeReport, header);
				string text = PlanText(plan);
				SimulationJson.WriteText(planPath, text);
				report["plan"] = Path.GetFullPath(planPath);
				report["plan_sha256"] = Sha256Hex(Encoding.UTF8.GetBytes(text));
				report["counts"] = plan["counts"];
				report["scene_dirty"] = scene.isDirty;
				report["scene_sha256_after"] = Sha256File(sceneFile);
				report["ok"] = true;
				return SimulationBatch.ExitOk;
			});
		}

		public static void Apply()
		{
			string outDir = SimulationBatch.GetArg("-tufuSimBakeOut");
			var manifest = new JObject { ["schema"] = ApplySchema, ["ok"] = false, ["pid"] = Process.GetCurrentProcess().Id, ["unity_version"] = Application.unityVersion };
			Finish(manifest, outDir, ApplyManifestFile, () =>
			{
				manifest["entered_play"] = SessionState.GetBool(EnteredPlayKey, false);
				string refusal = ProcessRefusal() ?? CallerRefusal(SimulationBatch.GetArg("-tufuSimBakeCaller"), Environment.GetEnvironmentVariable);
				if (refusal != null)
				{
					manifest["error"] = refusal;
					return SimulationBatch.ExitGuard;
				}
				string planPath = SimulationBatch.GetArg("-tufuSimBakePlan");
				if (string.IsNullOrEmpty(planPath) || !File.Exists(planPath) || string.IsNullOrEmpty(outDir))
				{
					manifest["error"] = "missing -tufuSimBakePlan <plan.json> or -tufuSimBakeOut <dir>";
					return SimulationBatch.ExitBadRequest;
				}
				byte[] planBytes = File.ReadAllBytes(planPath);
				manifest["plan"] = Path.GetFullPath(planPath);
				manifest["token"] = Sha256Hex(planBytes);
				refusal = TokenRefusal(planBytes, SimulationBatch.GetArg("-tufuSimBakeToken"));
				JObject plan = refusal == null ? JObject.Parse(Encoding.UTF8.GetString(planBytes)) : null;
				if (refusal == null && (string)plan["schema"] != PlanSchema) refusal = "refused: plan schema must be " + PlanSchema;
				string scenePath = plan != null ? (string)plan["scene"] : null;
				if (refusal == null && string.IsNullOrEmpty(scenePath)) refusal = "refused: the plan names no scene";
				if (refusal == null) refusal = GitRefusal(scenePath);
				string sceneFile = scenePath != null ? FullPath(scenePath) : null;
				string before = sceneFile != null ? Sha256File(sceneFile) : null;
				manifest["scene"] = scenePath;
				manifest["scene_sha256_before"] = before;
				if (refusal == null && before != (string)plan["scene_sha256"])
					refusal = "refused: " + scenePath + " is not the planned file (sha256 " + before + ", plan " + plan["scene_sha256"] + ")";
				if (refusal != null)
				{
					manifest["error"] = refusal;
					return SimulationBatch.ExitGuard;
				}
				Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
				ApplyResult result = ApplyPlan(SimulationBakeExport.SpawnersOf(scene), plan);
				manifest["written"] = result.Written;
				manifest["unchanged"] = result.Unchanged;
				manifest["skipped"] = result.Skipped;
				if (result.Refusal != null)
				{
					manifest["error"] = result.Refusal;
					return result.Written.Count > 0 ? SimulationBatch.ExitException : SimulationBatch.ExitGuard;
				}
				string backupDir = Path.Combine(outDir, "backup");
				Directory.CreateDirectory(backupDir);
				string backup = Path.Combine(backupDir, Path.GetFileName(sceneFile));
				File.Copy(sceneFile, backup, true);
				manifest["backup"] = backup;
				manifest["backup_sha256"] = Sha256File(backup);
				if ((string)manifest["backup_sha256"] != before)
				{
					manifest["error"] = "scene backup does not match the scene file";
					return SimulationBatch.ExitException;
				}
				if (result.Written.Count > 0 && !EditorSceneManager.SaveScene(scene))
				{
					manifest["error"] = "SaveScene failed";
					return SimulationBatch.ExitException;
				}
				manifest["scene_sha256_after"] = Sha256File(sceneFile);
				manifest["ok"] = true;
				return SimulationBatch.ExitOk;
			});
		}

		public static void SelfTest()
		{
			string outDir = SimulationBatch.GetArg("-tufuSimBakeOut");
			var report = new JObject { ["schema"] = SelfTestSchema, ["ok"] = false, ["pid"] = Process.GetCurrentProcess().Id, ["unity_version"] = Application.unityVersion };
			Finish(report, outDir, "bake_selftest.json", () => RunSelfTest(report));
		}

		private static void Finish(JObject report, string outDir, string file, Func<int> body)
		{
			int code;
			try
			{
				code = body();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				report["error"] = e.GetType().Name + ": " + e.Message;
				code = SimulationBatch.ExitException;
			}
			report["exit_code"] = code;
			if (!string.IsNullOrEmpty(outDir)) SimulationJson.WriteText(Path.Combine(outDir, file), report.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n");
			Debug.Log(LogTag + "bake tool " + file + ": exit " + code + (report["error"] != null && report["error"].Type != JTokenType.Null ? ", " + report["error"] : ""));
			EditorApplication.Exit(code);
		}

		// ---- self-test on an unsaved scene ----

		private sealed class Case
		{
			public string Name, Parent, Key;
			public bool Event, Vip, Dummy, HandBalance, NoDamage, InReport = true, ReportError;
			public int Count = 1;
			public double OldHealth, OldDamage, NewHealth, NewDamage;
			public string Action;
			public string[] Reasons = new string[0];
		}

		private static List<Case> Cases()
		{
			return new List<Case>
			{
				new Case { Name = "write_a", Key = "k_write_a", OldHealth = 100, OldDamage = 10, NewHealth = 250, NewDamage = 30, Action = ActionWrite },
				new Case { Name = "write_b", Parent = "Island", Key = "k_write_b", OldHealth = 1e6, OldDamage = 512.5, NewHealth = 1234567, NewDamage = 0.1 + 0.2, Action = ActionWrite },
				new Case { Name = "write_health_only", Key = "k_write_h", OldHealth = 7, OldDamage = 3, NewHealth = 8, NewDamage = 3, Action = ActionWrite },
				new Case { Name = "unchanged", Key = "k_unchanged", OldHealth = 50, OldDamage = 5, NewHealth = 50, NewDamage = 5, Action = ActionUnchanged },
				new Case { Name = "event", Key = "k_event", Event = true, OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "event" } },
				new Case { Name = "vip", Key = "k_vip", Vip = true, OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "vip" } },
				new Case { Name = "BossTower", Key = "k_boss", Count = 0, OldHealth = 1, OldDamage = 1, NewHealth = double.PositiveInfinity, NewDamage = 20, Action = ActionSkip, Reasons = new[] { "boss_tower", "non_finite" } },
				new Case { Name = "Spawner_Dummy", Key = "k_dummy", Dummy = true, OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "dummy" } },
				new Case { Name = "hand", Key = "k_hand", HandBalance = true, OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "hand_balance" } },
				new Case { Name = "nan", Key = "k_nan", OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = double.NaN, Action = ActionSkip, Reasons = new[] { "non_finite" } },
				new Case { Name = "missing", Key = "k_missing", InReport = false, OldHealth = 1, OldDamage = 1, Action = ActionSkip, Reasons = new[] { "not_in_report" } },
				new Case { Name = "error", Key = "k_error", ReportError = true, OldHealth = 1, OldDamage = 1, Action = ActionSkip, Reasons = new[] { "report_error" } },
				new Case { Name = "nostat", Key = "k_nostat", NoDamage = true, OldHealth = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "no_stat" } },
				new Case { Name = "dup", Key = "k_dup", OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "duplicate_id" } },
				new Case { Name = "dup", Key = "k_dup", OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip, Reasons = new[] { "duplicate_id" } },
				new Case { Name = "event_vip_hand", Key = "k_multi", Event = true, Vip = true, HandBalance = true, OldHealth = 1, OldDamage = 1, NewHealth = 2, NewDamage = 2, Action = ActionSkip,
					Reasons = new[] { "event", "vip", "hand_balance" } },
			};
		}

		private static EnemySpawner MakeSpawner(Case k, Dictionary<string, GameObject> parents)
		{
			var go = new GameObject(k.Name);
			if (k.Parent != null)
			{
				if (!parents.TryGetValue(k.Parent, out GameObject parent)) parents[k.Parent] = parent = new GameObject(k.Parent);
				go.transform.SetParent(parent.transform, false);
			}
			var spawner = go.AddComponent<EnemySpawner>();
			var so = new SerializedObject(spawner);
			so.FindProperty("config.Key").stringValue = k.Key;
			so.FindProperty("config.Count").intValue = k.Count;
			so.FindProperty("config.LocationNumber").intValue = 1;
			so.FindProperty("config.IsEvent").boolValue = k.Event;
			so.FindProperty("config.IsVip").boolValue = k.Vip;
			so.FindProperty("config.IsDummy").boolValue = k.Dummy;
			so.FindProperty("config.HandBalance").boolValue = k.HandBalance;
			SerializedProperty stats = so.FindProperty("config.Stats");
			var values = new List<(StatType, double)> { (StatType.AttackCooldown, 1.5), (StatType.Health, k.OldHealth) };
			if (!k.NoDamage) values.Add((StatType.Damage, k.OldDamage));
			values.Add((StatType.Health, 999)); // a second Health row: only the first one is the bake's
			stats.arraySize = values.Count;
			for (int i = 0; i < values.Count; i++)
			{
				SerializedProperty e = stats.GetArrayElementAtIndex(i);
				e.FindPropertyRelative("Type").intValue = (int)values[i].Item1;
				e.FindPropertyRelative("BaseValue").doubleValue = values[i].Item2;
			}
			so.ApplyModifiedPropertiesWithoutUndo();
			return spawner;
		}

		private static JObject SyntheticReport(List<Case> cases, List<EnemySpawner> spawners)
		{
			var rows = new JArray();
			var seen = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < cases.Count; i++)
			{
				string id = SimulationRuntimeProbes.SpawnerId(spawners[i]);
				if (!cases[i].InReport || !seen.Add(id)) continue;
				var row = new JObject { ["index"] = i, ["id"] = id };
				if (cases[i].ReportError) row["error"] = "InvalidOperationException: Sequence contains no matching element";
				else row["report"] = new JObject { ["NewHealth"] = SimulationRuntimeProbes.D(cases[i].NewHealth), ["NewDamage"] = SimulationRuntimeProbes.D(cases[i].NewDamage) };
				rows.Add(row);
			}
			rows.Add(new JObject { ["index"] = 999, ["id"] = ":not/in/scene|k_ghost", ["report"] = new JObject { ["NewHealth"] = SimulationRuntimeProbes.D(1), ["NewDamage"] = SimulationRuntimeProbes.D(1) } });
			return new JObject { ["schema"] = SimulationBakeExport.Schema, ["snapshots"] = "synthetic", ["spawners"] = rows };
		}

		private static (double, double?) Values(EnemySpawner s)
		{
			Stat damage = s.Config.Stats.FirstOrDefault(x => x.Type == StatType.Damage);
			return (s.Config.Stats.First(x => x.Type == StatType.Health).BaseValue, damage?.BaseValue);
		}

		private static int RunSelfTest(JObject report)
		{
			var checks = new JObject();
			bool all = true;
			void Check(string name, bool ok, object detail = null)
			{
				checks[name] = detail == null ? (JToken)ok : new JObject { ["ok"] = ok, ["detail"] = JToken.FromObject(detail) };
				all &= ok;
				if (!ok) Debug.LogError(LogTag + "bake self-test failed: " + name + (detail != null ? " " + detail : ""));
			}

			string mainFile = FullPath(MainScenePath);
			string mainBefore = Sha256File(mainFile);
			report["main_scene_sha256_before"] = mainBefore;

			// Guard
			Check("fresh_process_allowed", ProcessRefusal() == null, ProcessRefusal());
			SessionState.SetBool(EnteredPlayKey, true);
			Check("entered_play_refused", ProcessRefusal() != null);
			SessionState.SetBool(EnteredPlayKey, false);
			Func<string, string> none = _ => null;
			Check("caller_cli_allowed", CallerRefusal(CallerCli, none) == null);
			Check("caller_refresh_refused", CallerRefusal("refresh", none) != null);
			Check("caller_missing_refused", CallerRefusal(null, none) != null);
			foreach (string name in RefusedEnvironment)
				Check("env_" + name + "_refused", CallerRefusal(CallerCli, n => n == name ? "1" : null) != null);
			byte[] sample = Encoding.UTF8.GetBytes("{\"schema\": \"x\"}\n");
			Check("token_match_allowed", TokenRefusal(sample, Sha256Hex(sample)) == null);
			Check("token_mismatch_refused", TokenRefusal(sample, Sha256Hex(Encoding.UTF8.GetBytes("other"))) != null);
			Check("token_missing_refused", TokenRefusal(sample, null) != null);
			Check("git_clean_tracked_allowed", GitRefusal(MainScenePath) == null, GitRefusal(MainScenePath));
			string untracked = "Temp/TufuSimBake/untracked.unity";
			Directory.CreateDirectory(Path.GetDirectoryName(FullPath(untracked)));
			File.WriteAllText(FullPath(untracked), "x");
			Check("git_untracked_refused", GitRefusal(untracked) != null, GitRefusal(untracked));
			File.Delete(FullPath(untracked));
			SelfTestGitRepo(Check);
			Check("initialize_on_load", typeof(SimulationBakeTool).IsDefined(typeof(InitializeOnLoadAttribute), false));
			bool atLoad = RegisteredFrom.Contains("ProcessInitializeOnLoadAttributes");
			Check("play_hook_registered_at_load", atLoad, atLoad ? null : RegisteredFrom);
			OnPlayModeStateChanged(PlayModeStateChange.ExitingEditMode);
			Check("play_hook_marks_entered_play", ProcessRefusal() != null, ProcessRefusal());
			SessionState.SetBool(EnteredPlayKey, false);

			// Plan and write-back on a scene that is never saved
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			List<Case> cases = Cases();
			var parents = new Dictionary<string, GameObject>();
			var spawners = cases.Select(k => MakeSpawner(k, parents)).ToList();
			List<EnemySpawner> found = SimulationBakeExport.SpawnersOf(scene);
			Check("scene_spawners_found", found.Count == cases.Count && found.All(spawners.Contains), found.Count);
			JObject bakeReport = SyntheticReport(cases, spawners);
			JObject plan = BuildPlan(spawners, bakeReport, new JObject { ["scene"] = null, ["scene_sha256"] = null, ["code_rev"] = null });
			var rows = (JArray)plan["rows"];
			var wrong = new JArray();
			for (int i = 0; i < cases.Count; i++)
			{
				var row = (JObject)rows[i];
				string[] reasons = ((JArray)row["reasons"]).Select(x => (string)x).ToArray();
				if ((string)row["action"] != cases[i].Action || !reasons.SequenceEqual(cases[i].Reasons))
					wrong.Add(cases[i].Name + ": " + row["action"] + " [" + string.Join(",", reasons) + "]");
			}
			Check("plan_actions_and_reasons", wrong.Count == 0, wrong);
			Check("plan_unmatched_report_rows", plan["unmatched_report_rows"].Select(x => (string)x).SequenceEqual(new[] { ":not/in/scene|k_ghost" }), plan["unmatched_report_rows"]);
			Check("plan_first_health_row", (string)rows[0]["old_health_bits"] == SimulationRuntimeProbes.D(100));
			string planText = PlanText(plan);
			Check("plan_text_deterministic", PlanText(BuildPlan(spawners, bakeReport, new JObject { ["scene"] = null, ["scene_sha256"] = null, ["code_rev"] = null })) == planText);
			report["plan_counts"] = plan["counts"];

			var before = spawners.Select(Values).ToList();
			// A stale plan (one planned old value no longer in the scene) writes nothing.
			JObject stale = (JObject)plan.DeepClone();
			JObject staleRow = ((JArray)stale["rows"]).Cast<JObject>().Last(r => (string)r["action"] == ActionWrite);
			staleRow["old_damage_bits"] = SimulationRuntimeProbes.D(-1);
			ApplyResult refused = ApplyPlan(spawners, stale);
			Check("stale_plan_refused", refused.Refusal != null && refused.Written.Count == 0, refused.Refusal);
			Check("stale_plan_changed_nothing", spawners.Select(Values).SequenceEqual(before));

			ApplyResult applied = ApplyPlan(spawners, JObject.Parse(planText));
			report["written"] = applied.Written;
			Check("apply_ok", applied.Refusal == null, applied.Refusal);
			Check("apply_counts", applied.Written.Count == 3 && applied.Unchanged == 1 && applied.Skipped == cases.Count - 4,
				new[] { applied.Written.Count, applied.Unchanged, applied.Skipped });
			var after = spawners.Select(Values).ToList();
			var mismatch = new JArray();
			for (int i = 0; i < cases.Count; i++)
			{
				Case k = cases[i];
				(double h, double? d) expected = k.Action == ActionWrite ? (k.NewHealth, (double?)k.NewDamage) : before[i];
				if (BitConverter.DoubleToInt64Bits(after[i].Item1) != BitConverter.DoubleToInt64Bits(expected.h) || !Equals(after[i].Item2, expected.d))
					mismatch.Add(k.Name);
			}
			Check("apply_values", mismatch.Count == 0, mismatch);
			Check("second_health_row_untouched", spawners.All(s => s.Config.Stats.Last().BaseValue == 999));
			Check("scene_dirty_not_saved", scene.isDirty && string.IsNullOrEmpty(scene.path));
			Undo.PerformUndo();
			Check("undo_restores_all", spawners.Select(Values).SequenceEqual(before));
			Undo.PerformRedo();
			Check("redo_reapplies", spawners.Select(Values).SequenceEqual(after));

			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			Check("main_scene_untouched", Sha256File(mainFile) == mainBefore);
			Check("temp_scene_never_saved", string.IsNullOrEmpty(scene.path));
			report["checks"] = checks;
			report["ok"] = all;
			return all ? SimulationBatch.ExitOk : SimulationBatch.ExitException;
		}

		// GitRefusal on a throwaway repository under Temp/: clean, modified, staged-then-reverted, untracked, ignored.
		private static void SelfTestGitRepo(Action<string, bool, object> check)
		{
			string repo = FullPath("Temp/TufuSimBake/repo");
			if (Directory.Exists(repo)) Directory.Delete(repo, true);
			Directory.CreateDirectory(repo);
			const string commit = "-c user.name=t -c user.email=t@t -c commit.gpgsign=false -c core.hooksPath=/dev/null commit -q -m scene";
			string scene = Path.Combine(repo, "a.unity");
			File.WriteAllText(scene, "scene\n");
			File.WriteAllText(Path.Combine(repo, ".gitignore"), "ignored.unity\n");
			bool ready = Git(repo, "init -q").Item1 == 0 && Git(repo, "add -A").Item1 == 0 && Git(repo, commit).Item1 == 0;
			check("git_repo_ready", ready, null);
			if (!ready) return;
			string r = GitRefusal("a.unity", repo);
			check("git_repo_clean_allowed", r == null, r);
			File.WriteAllText(scene, "scene\nedit\n");
			r = GitRefusal("a.unity", repo);
			check("git_modified_refused", r != null && r.Contains("not clean"), r);
			Git(repo, "add a.unity");
			File.WriteAllText(scene, "scene\n");
			r = GitRefusal("a.unity", repo);
			check("git_staged_refused", r != null && r.Contains("not clean"), r);
			Git(repo, "reset -q");
			File.WriteAllText(Path.Combine(repo, "ignored.unity"), "x");
			r = GitRefusal("ignored.unity", repo);
			check("git_ignored_untracked_refused", r != null && r.Contains("not tracked"), r);
			r = GitRefusal("a.unity", repo);
			check("git_repo_clean_again", r == null, r);
			Directory.Delete(repo, true);
		}

		// ---- helpers ----

		private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

		private static long Bits(double v) => BitConverter.DoubleToInt64Bits(v);

		private static string ProjectDir() => Path.GetDirectoryName(Application.dataPath) ?? "";

		private static string FullPath(string projectRelative) => Path.Combine(ProjectDir(), projectRelative);

		public static string Sha256Hex(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
				return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
		}

		private static string Sha256File(string path) => path != null && File.Exists(path) ? Sha256Hex(File.ReadAllBytes(path)) : null;

		private static (int, string, string) Git(string dir, string args)
		{
			var info = new ProcessStartInfo("git", "-C \"" + dir + "\" " + args)
			{
				UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
			};
			using (Process p = Process.Start(info))
			{
				string output = p.StandardOutput.ReadToEnd();
				string error = p.StandardError.ReadToEnd();
				p.WaitForExit(30000);
				return (p.ExitCode, output, error);
			}
		}
	}
}
