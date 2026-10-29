using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Codebase.Simulation.Harness
{
	// The original SimulationConfig.BuildSpawnerBakeReport over every spawner of the loaded scenes when a job ends
	// (design 1.4.3, 3.5 X4, 7.3 C8). Day-mode spawners read config.TimeChunkSnapshots, which holds this run's full
	// day list; minute-mode ones read SimulationData.GetByPlayTime, so a transient SimulationData holding this run's
	// Snapshots is swapped into config.SimulationData for the bake and swapped back right after. bake.snapshots
	// "shipped" reads the shipped Simulation Data as on disk instead (config.TimeChunkSnapshots empty, as DP5).
	// bake_report.json carries every report field as the hex of its IEEE bits (DP5's encoding), bake_report.csv the
	// main fields as round-trip text. ED/SimulationBakeTool.cs plans the guarded write-back from the JSON.
	public static class SimulationBakeExport
	{
		public const string Schema = "tufu.sim.bake-report/1";
		public const string OutputName = "bake_report";
		public const string JsonFile = "bake_report.json", CsvFile = "bake_report.csv";
		public const string CsvHeader = "index,id,name,location,is_days,registered,active,enabled,is_event,is_vip,is_dummy,hand_balance,"
			+ "count,optimum_time,recalc_optimum_time,snapshot_time_chunk_index,snapshot_playtime,snapshot_source,armor_type,prev_health,"
			+ "prev_damage,player_dps,defense,new_health_raw,new_health,new_damage_raw,new_damage,error";

		public static bool Enabled(SimulationRunRequest.Job job)
		{
			return job.Outputs != null && job.Outputs.TryGetValue(OutputName, out bool on) && on;
		}

		public sealed class Row
		{
			public int Index;
			public string Id;
			public EnemySpawner Spawner;
			public bool Registered;
			public SpawnerBakeReportData Report;
			public string Source;
			public string Error;
		}

		// Every EnemySpawner component of a scene, inactive ones included (DP5's set), in hierarchy order.
		public static List<EnemySpawner> SpawnersOf(Scene scene)
		{
			var spawners = new List<EnemySpawner>();
			if (!scene.IsValid() || !scene.isLoaded) return spawners;
			foreach (GameObject root in scene.GetRootGameObjects()) spawners.AddRange(root.GetComponentsInChildren<EnemySpawner>(true));
			return spawners;
		}

		public static List<EnemySpawner> LoadedSceneSpawners()
		{
			var spawners = new List<EnemySpawner>();
			for (int i = 0; i < SceneManager.sceneCount; i++) spawners.AddRange(SpawnersOf(SceneManager.GetSceneAt(i)));
			return spawners;
		}

		// Bakes every spawner with the chosen snapshot lists in place; config is restored before returning.
		// "swap" describes what was swapped and that it was restored.
		public static List<Row> Bake(SimulationConfig config, IList<EnemySpawner> spawners, ICollection<EnemySpawner> registered, string snapshots,
			JObject swap)
		{
			SimulationData savedData = config.SimulationData;
			List<SimulationSnapshot> savedChunks = config.TimeChunkSnapshots;
			SimulationData transient = null;
			var rows = new List<Row>();
			try
			{
				if (snapshots == SimulationRunRequest.BakeOptions.SnapshotsShipped)
				{
					config.TimeChunkSnapshots = new List<SimulationSnapshot>();
					swap["day_list"] = "SimulationData.TimeChunkSnapshots (shipped; config.TimeChunkSnapshots empty)";
					swap["minute_list"] = "SimulationData.Snapshots (shipped)";
				}
				else
				{
					transient = ScriptableObject.CreateInstance<SimulationData>();
					transient.name = "TufuSimBakeTransient";
					transient.hideFlags = HideFlags.HideAndDontSave;
					transient.Snapshots = config.Snapshots;
					transient.TimeChunkSnapshots = config.TimeChunkSnapshots;
					config.SimulationData = transient;
					swap["day_list"] = "config.TimeChunkSnapshots (this run, untrimmed)";
					swap["minute_list"] = "transient SimulationData.Snapshots (this run)";
				}
				SimulationData lists = config.SimulationData;
				for (int i = 0; i < spawners.Count; i++)
				{
					EnemySpawner spawner = spawners[i];
					var row = new Row { Index = i, Spawner = spawner, Id = SimulationRuntimeProbes.SpawnerId(spawner), Registered = registered.Contains(spawner) };
					try
					{
						row.Report = config.BuildSpawnerBakeReport(spawner);
						row.Source = SimulationRuntimeProbes.Source(row.Report.Snapshot.Stats, lists);
					}
					catch (Exception e)
					{
						row.Error = e.GetType().Name + ": " + e.Message;
					}
					rows.Add(row);
				}
			}
			finally
			{
				config.SimulationData = savedData;
				config.TimeChunkSnapshots = savedChunks;
				if (transient != null) UnityEngine.Object.DestroyImmediate(transient);
			}
			swap["restored"] = ReferenceEquals(config.SimulationData, savedData) && ReferenceEquals(config.TimeChunkSnapshots, savedChunks);
			return rows;
		}

		// ---- serialization ----

		// A snapshot as the IEEE hex of every field (DP4's encoding, without the source tags): the exact inputs of
		// the bake, independent of the text round trip of simulation_data.json/timechunk_full.json.
		public static JObject SnapshotJson(SimulationSnapshot s)
		{
			Func<double, string> d = SimulationRuntimeProbes.D;
			var stats = new StringBuilder();
			foreach (SimulationStatData st in s.Stats)
				stats.Append(SimulationRuntimeProbes.I((int)st.Key)).Append(d(st.Value)).Append(d(st.StatsFromFeaturesFlat)).Append(d(st.PercentMultiplier))
					.Append(d(st.TotalFlatValue)).Append(d(st.TotalValue)).Append(d(st.CurrentGpm));
			var dps = new StringBuilder();
			foreach (SimulationDpsData v in s.DpsData) dps.Append(SimulationRuntimeProbes.I((int)v.WeaponType)).Append(d(v.Value));
			var def = new StringBuilder();
			foreach (SimulationDpsData v in s.DefenseData) def.Append(SimulationRuntimeProbes.I((int)v.WeaponType)).Append(d(v.Value));
			var items = new JArray();
			foreach (SimulationItemData it in s.Items ?? new List<SimulationItemData>()) items.Add(new JArray { it.Key, it.Level, it.Copies });
			return new JObject
			{
				["p"] = SimulationRuntimeProbes.F(s.Playtime),
				["tp"] = SimulationRuntimeProbes.F(s.TotalPlaytime),
				["loc"] = s.Location,
				["tci"] = s.TimeChunkIndex,
				["stats"] = stats.ToString(),
				["dps"] = dps.ToString(),
				["def"] = def.ToString(),
				["items"] = items,
			};
		}

		public static JObject ReportJson(SpawnerBakeReportData r, string source)
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
				["DefenseFromSnapshot"] = d(r.DefenseFromSnapshot),
				["Defense"] = d(r.Defense),
				["HealthRegenFromStats"] = d(r.HealthRegenFromStats),
				["FlatRegenFromStats"] = d(r.FlatRegenFromStats),
				["BattleRegen"] = d(r.BattleRegen),
				["PlayerEvasion"] = d(r.PlayerEvasion),
				["PlayerBlockChanceRaw"] = d(r.PlayerBlockChanceRaw),
				["PlayerBlockAmountRaw"] = d(r.PlayerBlockAmountRaw),
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
				["SnapshotSource"] = source,
			};
		}

		public static JObject RowJson(Row row)
		{
			EnemySpawnerConfig c = row.Spawner.Config;
			var o = new JObject
			{
				["index"] = row.Index,
				["id"] = row.Id,
				["name"] = row.Spawner.name,
				["registered"] = row.Registered,
				["active"] = row.Spawner.gameObject.activeInHierarchy,
				["enabled"] = row.Spawner.enabled,
				["location"] = c != null ? c.LocationNumber : 0,
				["is_days"] = c != null && c.IsDays,
				["is_event"] = c != null && c.IsEvent,
				["is_vip"] = c != null && c.IsVip,
				["is_dummy"] = c != null && c.IsDummy,
				["hand_balance"] = c != null && c.HandBalance,
				["count"] = c != null ? c.Count : 0,
			};
			if (row.Report != null) o["report"] = ReportJson(row.Report, row.Source);
			else o["error"] = row.Error;
			return o;
		}

		public static string CsvText(IList<Row> rows)
		{
			var sb = new StringBuilder(CsvHeader).Append('\n');
			foreach (Row row in rows)
			{
				EnemySpawnerConfig c = row.Spawner.Config;
				SpawnerBakeReportData r = row.Report;
				Stat health = c != null && c.Stats != null ? c.Stats.FirstOrDefault(s => s.Type == StatType.Health) : null;
				Stat damage = c != null && c.Stats != null ? c.Stats.FirstOrDefault(s => s.Type == StatType.Damage) : null;
				var cells = new List<string>
				{
					row.Index.ToString(), Csv(row.Id), Csv(row.Spawner.name), (c != null ? c.LocationNumber : 0).ToString(), B(c != null && c.IsDays),
					B(row.Registered), B(row.Spawner.gameObject.activeInHierarchy), B(row.Spawner.enabled), B(c != null && c.IsEvent),
					B(c != null && c.IsVip), B(c != null && c.IsDummy), B(c != null && c.HandBalance), (c != null ? c.Count : 0).ToString(),
					c != null ? SimulationJson.FormatFloat(c.OptimumTime) : "",
					r != null ? SimulationJson.FormatFloat(r.ReCalcOptimumTime) : "",
					r != null ? r.TimeChunkIndex.ToString() : "",
					r != null ? SimulationJson.FormatFloat(r.Playtime) : "",
					Csv(row.Source ?? ""),
					r != null ? ((int)r.ArmorType).ToString() : "",
					health != null ? SimulationJson.FormatDouble(health.BaseValue) : "",
					damage != null ? SimulationJson.FormatDouble(damage.BaseValue) : "",
					r != null ? SimulationJson.FormatDouble(r.PlayerDps) : "",
					r != null ? SimulationJson.FormatDouble(r.Defense) : "",
					r != null ? SimulationJson.FormatDouble(r.NewHealthRaw) : "",
					r != null ? SimulationJson.FormatDouble(r.NewHealth) : "",
					r != null ? SimulationJson.FormatDouble(r.NewDamageRaw) : "",
					r != null ? SimulationJson.FormatDouble(r.NewDamage) : "",
					Csv(row.Error ?? ""),
				};
				sb.Append(string.Join(",", cells)).Append('\n');
			}
			return sb.ToString();
		}

		private static string B(bool v) => v ? "1" : "0";

		private static string Csv(string s) => s == null ? "" : s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

		// Runs the bake for a finished job and writes both files; returns the manifest "bake" object. problem is set
		// when the report must not be trusted (the job then fails).
		public static JObject Export(SimulationRunRequest.Job job, SimulationConfig config, string jobDir, JObject timings, out string problem)
		{
			problem = null;
			string snapshots = job.Bake != null ? job.Bake.Snapshots : SimulationRunRequest.BakeOptions.SnapshotsRun;
			var block = new JObject { ["schema"] = Schema, ["snapshots"] = snapshots };
			int runSnapshots = config.Snapshots != null ? config.Snapshots.Count : 0;
			int runDays = config.TimeChunkSnapshots != null ? config.TimeChunkSnapshots.Count : 0;
			block["run_snapshots"] = runSnapshots;
			block["run_time_chunk_snapshots"] = runDays;
			if (snapshots == SimulationRunRequest.BakeOptions.SnapshotsRun && (runSnapshots == 0 || runDays == 0))
			{
				// GetSnapshotForBake would fall back to the shipped data without a word.
				problem = "this job has " + runSnapshots + " snapshots and " + runDays + " day snapshots; the bake needs both";
				block["error"] = problem;
				return block;
			}
			JObject inputs = null;
			if (snapshots == SimulationRunRequest.BakeOptions.SnapshotsRun)
				inputs = new JObject
				{
					["Snapshots"] = new JArray(config.Snapshots.Select(SnapshotJson).Cast<object>().ToArray()),
					["TimeChunkSnapshots"] = new JArray(config.TimeChunkSnapshots.Select(SnapshotJson).Cast<object>().ToArray()),
				};
			var sw = Stopwatch.StartNew();
			List<EnemySpawner> spawners = LoadedSceneSpawners();
			var registered = new HashSet<EnemySpawner>(GameContext.Get<EnemiesModel>().Spawners);
			var swap = new JObject();
			List<Row> rows = Bake(config, spawners, registered, snapshots, swap);
			timings["bake_report_ms"] = sw.Elapsed.TotalMilliseconds;
			block["swap"] = swap;
			if (!(bool)swap["restored"]) problem = "SimulationConfig snapshot lists were not restored after the bake";

			var json = new JObject
			{
				["schema"] = Schema,
				["snapshots"] = snapshots,
				["scenes"] = new JArray(Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path).Cast<object>().ToArray()),
				["run_snapshots"] = runSnapshots,
				["run_time_chunk_snapshots"] = runDays,
				["spawners"] = new JArray(rows.Select(RowJson).Cast<object>().ToArray()),
			};
			if (inputs != null) json["inputs"] = inputs;
			string jsonText = json.ToString(Newtonsoft.Json.Formatting.None) + "\n";
			string csvText = CsvText(rows);
			SimulationJson.WriteText(Path.Combine(jobDir, JsonFile), jsonText);
			SimulationJson.WriteText(Path.Combine(jobDir, CsvFile), csvText);
			block["files"] = new JObject
			{
				[JsonFile] = new JObject { ["sha256"] = SimulationHarness.Sha256(jsonText), ["bytes"] = Encoding.UTF8.GetByteCount(jsonText) },
				[CsvFile] = new JObject { ["sha256"] = SimulationHarness.Sha256(csvText), ["bytes"] = Encoding.UTF8.GetByteCount(csvText), ["rows"] = rows.Count },
			};
			block["spawners"] = rows.Count;
			block["registered"] = rows.Count(r => r.Registered);
			block["errors"] = rows.Count(r => r.Report == null);
			block["non_finite"] = rows.Count(r => r.Report != null && (!IsFinite(r.Report.NewHealth) || !IsFinite(r.Report.NewDamage)));
			block["registered_not_in_scenes"] = registered.Count(s => !spawners.Contains(s));
			return block;
		}

		private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
	}

	public sealed partial class SimulationHarness
	{
		// C8's hook (design 7.4): after C1's outputs and C3c's trace, before manifest.json is written.
		partial void RebuildJobExportBake(SimulationRunRequest.Job current, string jobDir, JObject jobManifest)
		{
			if (!SimulationBakeExport.Enabled(current)) return;
			JObject block = SimulationBakeExport.Export(current, config, jobDir, timings, out string problem);
			jobManifest["bake"] = block;
			if (problem != null) Fail(ExitException, "bake report: " + problem);
		}
	}
}
