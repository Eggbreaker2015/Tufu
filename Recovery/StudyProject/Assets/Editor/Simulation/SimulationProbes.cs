using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Codebase.Enemy;
using Codebase.Simulation.Harness;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.EditorTools
{
	// Edit-mode differential probes (design 3.4 DP1-DP5; unit C2). SimulationPlayModeDriver calls RunEdit
	// for a job with "probes" before that job enters Play, so the assets are the ones on disk (the asset
	// guard restores them after every Play). Results go to <job dir>/probes_edit.json; the formulas run
	// in SimulationRuntimeProbes except DP5, which needs the Main scene open in edit mode.
	internal static class SimulationProbes
	{
		private const string LogTag = "[TufuSim] ";
		private const string SimulationConfigPath = "Assets/MonoBehaviour/Simulation Config.asset";
		private const string MainScenePath = "Assets/Scenes/Main.unity";

		internal static void RunEdit(SimulationRunRequest request, SimulationRunRequest.Job job)
		{
			SimulationRuntimeProbes.Validate(job.Probes);
			if (job.Probes.Edit.Count == 0) return;
			JObject input = SimulationRuntimeProbes.LoadInput(job.Probes);
			var config = AssetDatabase.LoadAssetAtPath<SimulationConfig>(SimulationConfigPath);
			if (config == null) throw new InvalidOperationException("Simulation Config not found at " + SimulationConfigPath);
			var output = new JObject
			{
				["schema"] = SimulationRuntimeProbes.Schema,
				["mode"] = "edit",
				["input_sha256"] = job.Probes.InputSha256,
				["unity_version"] = Application.unityVersion,
			};
			var times = new JObject();
			foreach (string dp in job.Probes.Edit)
			{
				Beat(dp);
				var sw = Stopwatch.StartNew();
				switch (dp)
				{
					case "DP1": output["dp1"] = SimulationRuntimeProbes.Dp1(input["dp1"]); break;
					case "DP2": output["dp2"] = SimulationRuntimeProbes.Dp2(input["dp2"]); break;
					case "DP3": output["dp3"] = SimulationRuntimeProbes.Dp3(config.CombatBalanceConfig, input["dp3"]); break;
					case "DP4": output["dp4"] = SimulationRuntimeProbes.Dp4(config.SimulationData, input["dp4"]); break;
					case "DP5": output["dp5"] = Dp5(config); break;
				}
				times[dp] = sw.Elapsed.TotalMilliseconds;
				Debug.Log(LogTag + "probe " + dp + " (edit) " + sw.Elapsed.TotalMilliseconds.ToString("F0") + " ms");
			}
			string dir = request.JobDir(job);
			Directory.CreateDirectory(dir);
			File.WriteAllBytes(Path.Combine(dir, SimulationRuntimeProbes.EditFile), Encoding.UTF8.GetBytes(output.ToString(Newtonsoft.Json.Formatting.None) + "\n"));
			SimulationJson.WriteText(Path.Combine(dir, "probes_edit_timings.json"), times.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n");
		}

		private static void Beat(string dp)
		{
			SimulationHarness.ConfigureHeartbeat(Path.Combine(SimulationSandbox.TempDir, "heartbeat.json"), 5, null);
			SimulationHarness.WriteHeartbeat("edit:probes", null, "edit:probes:" + dp, 0, 0);
		}

		// ---- DP5: SimulationConfig.BuildSpawnerBakeReport for every spawner of Main, opened in edit mode ----

		private static JObject Dp5(SimulationConfig config)
		{
			Scene scene = SceneManager.GetSceneByPath(MainScenePath);
			bool opened = !scene.isLoaded;
			if (opened) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
			try
			{
				var rows = new JArray();
				var spawners = new List<EnemySpawner>();
				foreach (GameObject root in scene.GetRootGameObjects()) spawners.AddRange(root.GetComponentsInChildren<EnemySpawner>(true));
				foreach (EnemySpawner spawner in spawners)
				{
					var row = new JObject
					{
						["id"] = SimulationRuntimeProbes.SpawnerId(spawner),
						["active"] = spawner.gameObject.activeInHierarchy,
						["enabled"] = spawner.enabled,
					};
					try
					{
						row["report"] = Report(config.BuildSpawnerBakeReport(spawner), config);
					}
					catch (Exception e)
					{
						row["error"] = e.GetType().Name + ": " + e.Message;
					}
					rows.Add(row);
				}
				return new JObject { ["scene"] = MainScenePath, ["spawners"] = rows, ["config_time_chunk_snapshots"] = config.TimeChunkSnapshots != null ? config.TimeChunkSnapshots.Count : -1 };
			}
			finally
			{
				if (opened) EditorSceneManager.CloseScene(scene, true);
			}
		}

		private static JObject Report(SpawnerBakeReportData r, SimulationConfig config)
		{
			Func<double, string> d = SimulationRuntimeProbes.D;
			Func<float, string> f = SimulationRuntimeProbes.F;
			var candidates = new StringBuilder();
			foreach (SpawnerBakeArmorCandidateData c in r.ArmorCandidateRows)
				candidates.Append(SimulationRuntimeProbes.I((int)c.WeaponType)).Append(f(c.Resist)).Append(d(c.DpsByWeaponType)).Append(d(c.Score));
			return new JObject
			{
				["Key"] = r.Key,
				["Name"] = r.Name,
				["LocationNumber"] = r.LocationNumber,
				["TimeChunkIndex"] = r.TimeChunkIndex,
				["Playtime"] = f(r.Playtime),
				["OptimumTime"] = f(r.OptimumTime),
				["ReCalcOptimumTime"] = f(r.ReCalcOptimumTime),
				["PrevHealth"] = d(r.PrevHealth),
				["PrevDamage"] = d(r.PrevDamage),
				["Count"] = r.Count,
				["AverageFightTime"] = f(r.AverageFightTime),
				["ArmorType"] = (int)r.ArmorType,
				["ArmorResistRaw"] = f(r.ArmorResistRaw),
				["ArmorResistUsed"] = f(r.ArmorResistUsed),
				["PreferredWeaponType"] = (int)r.PreferredWeaponType,
				["CritChanceRaw"] = d(r.CritChanceRaw),
				["CritDamageRaw"] = d(r.CritDamageRaw),
				["CritChance"] = d(r.CritChance),
				["CritDamage"] = d(r.CritDamage),
				["DpsByArmorType"] = d(r.DpsByArmorType),
				["PlayerDpsBeforeCrit"] = d(r.PlayerDpsBeforeCrit),
				["CritDps"] = d(r.CritDps),
				["PlayerDps"] = d(r.PlayerDps),
				["Defense"] = d(r.Defense),
				["HealthRegenFromStats"] = d(r.HealthRegenFromStats),
				["FlatRegenFromStats"] = d(r.FlatRegenFromStats),
				["BattleRegen"] = d(r.BattleRegen),
				["PlayerEvasion"] = d(r.PlayerEvasion),
				["PlayerBlockChance"] = d(r.PlayerBlockChance),
				["PlayerBlockAmount"] = d(r.PlayerBlockAmount),
				["BlockDamageMultiplier"] = d(r.BlockDamageMultiplier),
				["EnemiesCount"] = f(r.EnemiesCount),
				["DifficultyBeforeValidation"] = f(r.DifficultyBeforeValidation),
				["DifficultyUsed"] = f(r.DifficultyUsed),
				["NeedTotalHpBaseHealth"] = d(r.NeedTotalHpBaseHealth),
				["NeedTotalHp"] = d(r.NeedTotalHp),
				["NeedDps"] = d(r.NeedDps),
				["NeedDpsFromOneUnit"] = d(r.NeedDpsFromOneUnit),
				["AttackCooldownBase"] = d(r.AttackCooldownBase),
				["HitFrequency"] = d(r.HitFrequency),
				["NeedDamageFromOneUnit"] = d(r.NeedDamageFromOneUnit),
				["IncomingDamagePercentMultiplier"] = d(r.IncomingDamagePercentMultiplier),
				["PlayerAbilityDpsMultiplier"] = d(r.PlayerAbilityDpsMultiplier),
				["PlayerAblityEhpMultiplier"] = d(r.PlayerAblityEhpMultiplier),
				["NewDamageRaw"] = d(r.NewDamageRaw),
				["NewDamage"] = d(r.NewDamage),
				["NewHealthRaw"] = d(r.NewHealthRaw),
				["NewHealth"] = d(r.NewHealth),
				["Candidates"] = candidates.ToString(),
				["SnapshotSource"] = SimulationRuntimeProbes.Source(r.Snapshot.Stats, config.SimulationData, config.TimeChunkSnapshots),
			};
		}
	}
}
