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
using Codebase.Balance;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Services.Damage;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Newtonsoft.Json.Linq;
using UniMob;
using UnityEngine;

namespace Codebase.Simulation.Harness
{
	// Differential probes (design 3.4; unit C2): the game's own formula code evaluated on input grids
	// that Python generates (tufu_balance.unity.probes) and compares with the oracles. Every number
	// crosses the boundary as the hex of its IEEE bits (doubles x16, floats x8), concatenated per grid.
	// DP1-DP4 are pure and run in edit mode (ED/SimulationProbes.cs) and, for DP3, again in Play;
	// DP6 and DP7 need the running game and run when a probe job finishes.
	public static class SimulationRuntimeProbes
	{
		public const string Schema = "tufu.sim.probes/1";
		public const string EditFile = "probes_edit.json", RuntimeFile = "probes_runtime.json";
		public static readonly string[] EditProbes = { "DP1", "DP2", "DP3", "DP4", "DP5" };
		public static readonly string[] PlayProbes = { "DP3", "DP6", "DP7" };

		// <worktree>/Recovery/StudyProject/Assets -> <worktree>
		public static string RepositoryRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

		public static JObject LoadInput(SimulationRunRequest.ProbeOptions options)
		{
			if (string.IsNullOrEmpty(options.Input) || Path.IsPathRooted(options.Input) || options.Input.Contains(".."))
				throw new InvalidDataException("probes.input must be a path inside the repository: " + options.Input);
			string path = Path.Combine(RepositoryRoot, options.Input);
			byte[] bytes = File.ReadAllBytes(path);
			string sha = Sha256Hex(bytes);
			if (!string.Equals(sha, options.InputSha256, StringComparison.Ordinal))
				throw new InvalidDataException("probes input " + path + " has sha256 " + sha + ", the request expects " + options.InputSha256);
			return JObject.Parse(Encoding.UTF8.GetString(bytes));
		}

		public static string Sha256Hex(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
				return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
		}

		public static void Validate(SimulationRunRequest.ProbeOptions options)
		{
			foreach (string dp in options.Edit)
				if (!EditProbes.Contains(dp)) throw new InvalidDataException("probes.edit: unknown probe " + dp);
			foreach (string dp in options.Runtime)
				if (!PlayProbes.Contains(dp)) throw new InvalidDataException("probes.runtime: unknown probe " + dp);
		}

		// ---- hex codec ----

		public static string D(double v) => BitConverter.DoubleToInt64Bits(v).ToString("x16");

		public static string F(float v) => BitConverter.SingleToInt32Bits(v).ToString("x8");

		public static string I(int v) => v.ToString("x8");

		public static double[] Doubles(JToken hex)
		{
			string s = (string)hex;
			var values = new double[s.Length / 16];
			for (int i = 0; i < values.Length; i++)
				values[i] = BitConverter.Int64BitsToDouble(long.Parse(s.Substring(i * 16, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
			return values;
		}

		public static float[] Floats(JToken hex)
		{
			string s = (string)hex;
			var values = new float[s.Length / 8];
			for (int i = 0; i < values.Length; i++)
				values[i] = BitConverter.Int32BitsToSingle(int.Parse(s.Substring(i * 8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
			return values;
		}

		private static string Error(Exception e) => "error:" + e.GetType().Name;

		// ---- DP1: Stat.CalculateSpecificValue and the 0.8 cap ----

		public static JObject Dp1(JToken input)
		{
			int[] types = input["types"].ToObject<int[]>();
			double[] x = Doubles(input["x"]);
			var specific = new JObject();
			var clamped = new JObject();
			foreach (int t in types)
			{
				var a = new StringBuilder(x.Length * 16);
				var b = new StringBuilder(x.Length * 16);
				foreach (double v in x)
				{
					double s = Stat.CalculateSpecificValue((StatType)t, v);
					a.Append(D(s));
					b.Append(D(Stat.ClampSpecificValue((StatType)t, s)));
				}
				specific[t.ToString(CultureInfo.InvariantCulture)] = a.ToString();
				clamped[t.ToString(CultureInfo.InvariantCulture)] = b.ToString();
			}
			return new JObject { ["specific"] = specific, ["clamped"] = clamped };
		}

		// ---- DP2: defence curve, its inverse, battle regen, weapon type -> defence stat ----

		public static JObject Dp2(JToken input)
		{
			double[] a = Doubles(input["apply"]["attack"]), d = Doubles(input["apply"]["defense"]);
			var apply = new StringBuilder();
			for (int i = 0; i < a.Length; i++) apply.Append(D(DamageCalculator.ApplyDamageDefense(a[i], d[i])));
			double[] target = Doubles(input["solve"]["target"]), def = Doubles(input["solve"]["defense"]), pct = Doubles(input["solve"]["percent"]);
			var solve = new StringBuilder();
			for (int i = 0; i < target.Length; i++) solve.Append(D(DamageCalculator.SolveAttackDamageForIncomingDamage(target[i], def[i], pct[i])));
			double[] hr = Doubles(input["regen"]["health_regen"]), fr = Doubles(input["regen"]["lifesteal_flat"]), diff = Doubles(input["regen"]["difficulty"]);
			var regen = new StringBuilder();
			for (int i = 0; i < hr.Length; i++) regen.Append(D(DamageCalculator.GetBattleRegen(hr[i], fr[i], diff[i])));
			var defence = new JArray();
			foreach (int wt in input["weapon_types"].ToObject<int[]>())
			{
				var row = new JArray { wt };
				foreach (bool percent in new[] { false, true })
				{
					try
					{
						row.Add((int)DamageCalculator.WeaponTypeToDefence((WeaponType)wt, percent));
					}
					catch (Exception e)
					{
						row.Add(Error(e));
					}
				}
				defence.Add(row);
			}
			return new JObject { ["apply"] = apply.ToString(), ["solve"] = solve.ToString(), ["regen"] = regen.ToString(), ["defence"] = defence };
		}

		// ---- DP3: BalanceInterpolation.Evaluate per curve and CombatBalanceConfig.GetInterpolationCoefficient ----

		public static JObject Dp3(CombatBalanceConfig balance, JToken input)
		{
			float[] t = Floats(input["t"]);
			var curves = new JArray();
			foreach (BalanceInterpolation curve in balance.Interpolations.Interpolations)
			{
				var sb = new StringBuilder(t.Length * 8);
				foreach (float x in t) sb.Append(F(curve.Evaluate(x)));
				curves.Add(new JObject { ["name"] = curve.Name, ["type"] = (int)curve.Type, ["use"] = curve.Use, ["values"] = sb.ToString() });
			}
			var coef = new JObject();
			foreach (int type in input["types"].ToObject<int[]>())
			{
				var sb = new StringBuilder(t.Length * 8);
				foreach (float x in t) sb.Append(F(balance.GetInterpolationCoefficient(x, (BalanceInterpolationType)type)));
				coef[type.ToString(CultureInfo.InvariantCulture)] = sb.ToString();
			}
			var byRarity = new JObject();
			foreach (int type in input["types"].ToObject<int[]>())
			foreach (int rarity in input["rarities"].ToObject<int[]>())
			{
				var sb = new StringBuilder(t.Length * 8);
				foreach (float x in t) sb.Append(F(balance.GetInterpolationCoefficient(x, (BalanceInterpolationType)type, (Rarity)rarity)));
				byRarity[type.ToString(CultureInfo.InvariantCulture) + ":" + rarity.ToString(CultureInfo.InvariantCulture)] = sb.ToString();
			}
			return new JObject { ["curves"] = curves, ["coef"] = coef, ["coef_rarity"] = byRarity };
		}

		// ---- DP4: SimulationData.GetByTimeChunkIndex / GetByPlayTime ----

		public static JObject Dp4(SimulationData data, JToken input)
		{
			var tc = new JArray();
			foreach (float x in Floats(input["tc"])) tc.Add(Snapshot(data.GetByTimeChunkIndex(x), data));
			var early = new JArray();
			foreach (float x in Floats(input["tc_early"])) early.Add(Snapshot(data.GetByTimeChunkIndex(x, data.Snapshots), data));
			var play = new JArray();
			foreach (float x in Floats(input["playtime"])) play.Add(Snapshot(data.GetByPlayTime(x), data));
			return new JObject { ["tc"] = tc, ["tc_early"] = early, ["playtime"] = play };
		}

		// Where a returned snapshot's lists come from: "t:<i>" / "s:<i>" (TimeChunkSnapshots / Snapshots
		// element i, the same list object) or null (built by interpolation).
		public static string Source(object list, SimulationData data, IList<SimulationSnapshot> configTimeChunks = null)
		{
			if (list == null) return null;
			for (int i = 0; i < data.TimeChunkSnapshots.Count; i++)
				if (ReferenceEquals(data.TimeChunkSnapshots[i].Stats, list) || ReferenceEquals(data.TimeChunkSnapshots[i].Items, list)) return "t:" + i;
			for (int i = 0; i < data.Snapshots.Count; i++)
				if (ReferenceEquals(data.Snapshots[i].Stats, list) || ReferenceEquals(data.Snapshots[i].Items, list)) return "s:" + i;
			if (configTimeChunks != null)
				for (int i = 0; i < configTimeChunks.Count; i++)
					if (ReferenceEquals(configTimeChunks[i].Stats, list) || ReferenceEquals(configTimeChunks[i].Items, list)) return "c:" + i;
			return null;
		}

		public static JObject Snapshot(SimulationSnapshot s, SimulationData data, IList<SimulationSnapshot> configTimeChunks = null)
		{
			var stats = new StringBuilder();
			foreach (SimulationStatData st in s.Stats)
				stats.Append(I((int)st.Key)).Append(D(st.Value)).Append(D(st.StatsFromFeaturesFlat)).Append(D(st.PercentMultiplier))
					.Append(D(st.TotalFlatValue)).Append(D(st.TotalValue)).Append(D(st.CurrentGpm));
			var dps = new StringBuilder();
			foreach (SimulationDpsData v in s.DpsData) dps.Append(I((int)v.WeaponType)).Append(D(v.Value));
			var def = new StringBuilder();
			foreach (SimulationDpsData v in s.DefenseData) def.Append(I((int)v.WeaponType)).Append(D(v.Value));
			var items = new JArray();
			foreach (SimulationItemData it in s.Items) items.Add(new JArray { it.Key, it.Level, it.Copies });
			return new JObject
			{
				["src"] = Source(s.Stats, data, configTimeChunks),
				["items_src"] = Source(s.Items, data, configTimeChunks),
				["p"] = F(s.Playtime),
				["tp"] = F(s.TotalPlaytime),
				["loc"] = s.Location,
				["tci"] = s.TimeChunkIndex,
				["stats"] = stats.ToString(),
				["dps"] = dps.ToString(),
				["def"] = def.ToString(),
				["items"] = items,
			};
		}

		// ---- spawner identity (same text as SimulationHarness.HierarchyPath + "|" + Key) ----

		public static string SpawnerId(EnemySpawner spawner)
		{
			var names = new List<string>();
			for (Transform x = spawner.transform; x != null; x = x.parent) names.Add(x.name);
			names.Reverse();
			return spawner.gameObject.scene.name + ":" + string.Join("/", names) + "|" + (spawner.Config != null ? spawner.Config.Key : "");
		}

		// ---- DP6: EnemySpawner.ChargesTotal, BaseCooldown, SimulationSystem.GetSpawnerChargesLimit ----

		public static JObject Dp6()
		{
			const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
			MethodInfo limit = typeof(SimulationSystem).GetMethod("GetSpawnerChargesLimit", Private);
			MethodInfo usesAb = typeof(EnemySpawner).GetMethod("CanUseSpawnerCooldownsAb", Private);
			FieldInfo abField = typeof(EnemySpawner).GetField("abTestService", Private);
			SimulationSystem system = SimulationSystem.Instance;
			var rows = new JArray();
			ABTestService service = null;
			foreach (EnemySpawner s in GameContext.Get<EnemiesModel>().Spawners)
			{
				if (service == null) service = (ABTestService)abField.GetValue(s);
				EnemySpawnerConfig c = s.Config;
				rows.Add(new JArray
				{
					SpawnerId(s), s.ChargesTotal, F(s.BaseCooldown), (int)limit.Invoke(system, new object[] { s }), (bool)usesAb.Invoke(s, null),
					c.IsEliteBoss, c.LocationNumber, c.ItemReward != null ? (int)c.ItemReward.Rarity : -1, c.DisableAfterFirstClear, c.Count,
				});
			}
			var ab = new JObject();
			foreach (bool elite in new[] { false, true })
			{
				var minutes = new JArray();
				var charges = new JArray();
				for (int location = 1; location <= 20; location++)
				{
					minutes.Add(F(service.GetSpawnerCooldownSeconds(elite, location) / 60f));
					charges.Add(service.GetSpawnerCharges(elite, location, 1));
				}
				ab[elite ? "elite" : "non_elite"] = new JObject { ["cooldown_minutes"] = minutes, ["charges_per_unit"] = charges };
			}
			FastRPMParamsData fast = ABTestData.FastRPMParams.GetCurrentValue();
			ab["fast_rpm"] = new JArray { F(fast.fightTimeSeconds), F(fast.maxHealthLossMultiplier) };
			return new JObject
			{
				["columns"] = new JArray("id", "charges_total", "base_cooldown", "charges_limit", "uses_ab", "is_elite", "location", "item_rarity", "dafc", "count"),
				["rows"] = rows,
				["ab"] = ab,
			};
		}

		// ---- DP7: ItemModel.Damage, Armor, DPS (and LevelsToEvolve) ----

		public static JObject Dp7(JToken input)
		{
			var configs = new Dictionary<string, ItemConfig>();
			foreach (ItemConfig config in GameContext.Get<ItemsProvider>().Items)
				if (config != null && !configs.ContainsKey(config.Key)) configs[config.Key] = config;
			var rows = new JArray();
			var missing = new JArray();
			using (var lifetime = new LifetimeController())
			{
				foreach (JToken sample in (JArray)input["samples"])
				{
					string key = (string)sample[0];
					int level = (int)sample[1], copies = (int)sample[2];
					bool evolved = (int)sample[3] != 0;
					if (!configs.TryGetValue(key, out ItemConfig config))
					{
						missing.Add(key);
						rows.Add(new JArray { "missing", "missing", "missing", "missing" });
						continue;
					}
					var item = new ItemModel(lifetime.Lifetime, config);
					item.Evolved = evolved;
					item.Level = level;
					item.Copies = copies;
					rows.Add(new JArray { Try(() => D(item.Damage)), Try(() => D(item.Armor)), Try(() => D(item.DPS)), Try(() => item.LevelsToEvolve.ToString(CultureInfo.InvariantCulture)) });
				}
			}
			return new JObject { ["columns"] = new JArray("damage", "armor", "dps", "levels_to_evolve"), ["rows"] = rows, ["missing_keys"] = missing, ["provider_keys"] = configs.Count };
		}

		private static string Try(Func<string> f)
		{
			try
			{
				return f();
			}
			catch (Exception e)
			{
				return Error(e);
			}
		}

		// ---- Play-mode runner (called from SimulationHarness when a probe job finishes) ----

		// Returns the manifest's "probes" object; wall times go to "times" (timings.json), so the manifest
		// stays byte-comparable.
		public static JObject RunPlay(SimulationRunRequest.Job job, string jobDir, JObject times)
		{
			Validate(job.Probes);
			var summary = new JObject { ["input"] = job.Probes.Input, ["input_sha256"] = job.Probes.InputSha256 };
			string editPath = Path.Combine(jobDir, EditFile);
			if (File.Exists(editPath)) summary["edit"] = new JObject { ["file"] = EditFile, ["sha256"] = Sha256Hex(File.ReadAllBytes(editPath)) };
			if (job.Probes.Runtime.Count == 0) return summary;
			JObject input = LoadInput(job.Probes);
			var output = new JObject { ["schema"] = Schema, ["mode"] = "play", ["input_sha256"] = job.Probes.InputSha256, ["unity_version"] = Application.unityVersion };
			foreach (string dp in job.Probes.Runtime)
			{
				var sw = Stopwatch.StartNew();
				switch (dp)
				{
					case "DP3": output["dp3"] = Dp3(GameContext.Get<CombatBalanceConfig>(), input["dp3"]); break;
					case "DP6": output["dp6"] = Dp6(); break;
					case "DP7": output["dp7"] = Dp7(input["dp7"]); break;
				}
				times["probe_" + dp + "_ms"] = sw.Elapsed.TotalMilliseconds;
				SimulationHarness.Beat(true);
			}
			byte[] bytes = Encoding.UTF8.GetBytes(output.ToString(Newtonsoft.Json.Formatting.None) + "\n");
			File.WriteAllBytes(Path.Combine(jobDir, RuntimeFile), bytes);
			summary["runtime"] = new JObject { ["file"] = RuntimeFile, ["sha256"] = Sha256Hex(bytes), ["probes"] = new JArray(job.Probes.Runtime) };
			return summary;
		}
	}

	public sealed partial class SimulationHarness
	{
		partial void RebuildJobRunProbes(SimulationRunRequest.Job current, string jobDir, JObject jobManifest)
		{
			if (current.Probes == null) return;
			jobManifest["probes"] = SimulationRuntimeProbes.RunPlay(current, jobDir, timings);
		}
	}
}
