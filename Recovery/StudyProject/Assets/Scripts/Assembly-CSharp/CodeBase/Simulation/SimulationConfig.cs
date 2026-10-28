using System.Collections.Generic;
using Codebase.Balance;
using Codebase.Enemy;
using Codebase.Location;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Linq;
using Codebase.Unit;
using Codebase.Services.Damage;
using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation
{
	public partial class SimulationConfig : ScriptableObject
	{
		private const string GROUP_DEPENDENCIES = "Dependencies";
		private const string BOT_PROFILE = "bot_profile";
		private const string MINIMAP_SETTINGS = "minimap_settings";
		private const string STOP_SETTINGS = "stop_settings";
		private const string BAKE_SETTINGS = "BakeSettings";
		public const int MaxSimultaneousAttackers = 4;
		public SimulationData SimulationData;
		public LocationsConfig LocationsConfig;
		public CombatBalanceConfig CombatBalanceConfig;
		public Texture2D MinimapOverlay;
		public Vector2 MinimapWorldCenter;
		public float MinimapWorldScale;
		public float AdsRewardMultiplierInMinutes;
		public float AdsIntervalMinutes;
		public float[] LifeCycleInMinutes;
		public bool UseSpawnerCharges;
		public float SpawnerChargesChunkMinutes;
		public float TimeChunkDurationMinutes;
		public float EnemyHpDamageMultiplier;
		[HideInInspector]
		public Texture2D HeatmapTexture;
		public bool ShowDifficultyMap;
		public bool ShowMapVisualizer;
		public List<DifficultyThreshold> DifficultyThresholds;
		public bool IsRunning;
		public bool StopAtLocation;
		public int TargetLocationNumber;
		public bool StopAtDay;
		public int TargetDay;
		public bool CrystalSimulation;
		public bool GraphSimulation;
		public bool DisableUi;
		public bool AutoGambling;
		public bool AutoBalanceItems;
		public bool AutoBalanceAmulet;
		public bool IsLog;
		public bool IsSmartPath;
		public bool IsDynamicResourcePriority;
		public bool IsCapOptimumTime;
		public bool TrimSimulationData;
		public int EarlySpawnerOptimumTimeOffsetStartLocation;
		public float EarlySpawnerOptimumTimeOffsetRelativePart;
		public float EarlySpawnerOptimumTimeOffsetMultiplier;
		[HideInInspector]
		public bool handTick;
		public int TimeStep;
		public int SnapshotInterval;
		[Range(1f, 500f)]
		public int SimulationSpeed;
		public bool UseFastTick;
		public int FastTickStartChunkOffset;
		public float CurrentPlaytime;
		public float CurrentTotalPlaytime;
		public List<SimulationSnapshot> Snapshots;
		[FormerlySerializedAs("SpawnerChargesChunkSnapshots")]
		public List<SimulationSnapshot> TimeChunkSnapshots;
		public List<AmuletLevelBalanceData> AmuletLevelBalance;
		[TextArea(10, 45)]
		public string Log;
		public List<SpawnerBakeInfo> SpawnerBakeInfos;
		public List<RewardEntry> RewardList;
		public bool ShowBossDifficultCurve;
		public AnimationCurve curveBossDifficult;
		private bool canRebake => false;
		private bool canApplyBalance => false;

		[SimulationRebuild(Provenance.Null)]
		public void ResetCombatConfigValues()
		{
		}

		// Inspector button: one hand tick (SimulationSystem.OnUpdate consumes the flag).
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.hand_tick")]
		public void Tick()
		{
			handTick = true;
		}

		[SimulationRebuild(Provenance.Null)]
		public void ResetAmuletLevelBalance(int levelsCount)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		public void SetAmuletLevelBalance(int level, double experience, Dictionary<string, double> soulRewards)
		{
		}

		// Day snapshots kept in the exported data (G19): day 0, then i_{n+1} = max(i_n + 1, ceil(1.1f * i_n)) in
		// float32; the last day is kept only when the rule reaches it.
		[SimulationRebuild(Provenance.Inferred, "simulation.snapshot_trim")]
		private List<SimulationSnapshot> GetTrimmedTimeChunkSnapshots()
		{
			var trimmed = new List<SimulationSnapshot>();
			if (TimeChunkSnapshots == null) return trimmed;
			int next = 0;
			foreach (SimulationSnapshot snapshot in TimeChunkSnapshots)
			{
				int day = snapshot.TimeChunkIndex;
				if (day < next) continue;
				trimmed.Add(snapshot);
				next = Math.Max(day + 1, (int)Mathf.Ceil((float)(1.1f * day)));
			}
			return trimmed;
		}

		public SpawnerBakeReportData BuildSpawnerBakeReport(EnemySpawner spawner)
		{
			return BuildSpawnerBakeReport(spawner, spawner.Config.OptimumTime, GetReCalcOptimumTime(spawner), spawner.Config.IsDays);
		}

		public double CalculateBakedSpawnerHealth(EnemySpawner spawner, float optimumDay)
		{
			return BuildSpawnerBakeReport(spawner, optimumDay, optimumDay, true).NewHealth;
		}

		private SpawnerBakeReportData BuildSpawnerBakeReport(EnemySpawner spawner, float optimumTime, float reCalcOptimumTime, bool isDays)
		{ // Original ELF0x13db16c plus all19 actual predicates/captured armor selectors.
			var c = spawner.Config;
			var previousHealth = c.Stats.First(stat => stat.Type == StatType.Health);
			var previousDamage = c.Stats.First(stat => stat.Type == StatType.Damage);
			float day = isDays ? reCalcOptimumTime : reCalcOptimumTime / 60f;
			var snapshot = GetSnapshotForBake(reCalcOptimumTime, isDays);
			float abilityDps = CombatBalanceConfig.GetInterpolationCoefficient(day, (Balance.BalanceInterpolationType)4);
			float abilityEhp = CombatBalanceConfig.GetInterpolationCoefficient(day, (Balance.BalanceInterpolationType)5);
			double rawCritChance = snapshot.Stats.First(stat => stat.Key == StatType.CritChance).TotalValue;
			double rawCritDamage = snapshot.Stats.First(stat => stat.Key == StatType.CritDamagePercent).TotalValue;
			double critChance = Stat.CalculateSpecificValue(StatType.CritChance, rawCritChance);
			double critDamage = Stat.CalculateSpecificValue(StatType.CritDamagePercent, rawCritDamage);
			var rows = c.ArmorConfig.Armors.Select(armor => new SpawnerBakeArmorCandidateData { WeaponType = armor.Type, Resist = armor.Resist, DpsByWeaponType = snapshot.DpsData.FirstOrDefault(value => value.WeaponType == armor.Type).Value, Score = snapshot.DpsData.FirstOrDefault(value => value.WeaponType == armor.Type).Value * (double)(1f - armor.Resist) }).OrderByDescending(row => row.Score).ToList();
			var chosenArmor = c.ArmorConfig.Armors.First(armor => armor.Type == rows.First().WeaponType);
			double dps = snapshot.DpsData.First(value => value.WeaponType == chosenArmor.Type).Value;
			var location = LocationsConfig.Locations.First(value => value.Number == c.LocationNumber);
			float usedResist = Math.Clamp(chosenArmor.Resist, -0.9f, 0.9f);
			double defense = snapshot.DefenseData.First(value => value.WeaponType == location.PreferredWeaponType).Value;
			double healthRegen = snapshot.Stats.First(stat => stat.Key == StatType.HealthRegen).TotalValue;
			double flatRegen = snapshot.Stats.First(stat => stat.Key == StatType.LifestealFlat).TotalValue;
			double evasion = Stat.CalculateSpecificValue(StatType.Evasion, snapshot.Stats.First(stat => stat.Key == StatType.Evasion).TotalValue);
			double rawBlockChance = snapshot.Stats.First(stat => stat.Key == StatType.BlockChance).TotalValue;
			double rawBlockAmount = snapshot.Stats.First(stat => stat.Key == StatType.BlockAmount).TotalValue;
			double blockChance = Stat.CalculateSpecificValue(StatType.BlockChance, rawBlockChance);
			double blockAmount = Stat.CalculateSpecificValue(StatType.BlockAmount, rawBlockAmount);
			float enemies = Math.Min(4f, (float)unchecked(c.Count + 1) / 2f);
			float rawDifficulty = c.Difficulty;
			float difficulty = rawDifficulty <= 0f ? 1f : rawDifficulty;
			double battleRegen = DamageCalculator.GetBattleRegen(healthRegen, flatRegen, (double)difficulty);
			double baseHealth = snapshot.Stats.First(stat => stat.Key == StatType.Health).TotalValue;
			float fightTime = c.AverageFightTime;
			var attackCooldown = c.Stats.First(stat => stat.Type == StatType.AttackCooldown);
			double neededHealth = baseHealth * (double)difficulty;
			double neededDps = neededHealth / (double)fightTime;
			double cooldown = attackCooldown.BaseValue;
			double neededDpsPerUnit = (battleRegen + neededDps) / (double)enemies;
			double frequency = cooldown / (1.0 - evasion);
			double blockMultiplier = 1.0 - blockChance * blockAmount;
			double neededDamage = (neededDpsPerUnit * frequency) / blockMultiplier;
			double incomingPercent = 1.0 / ((double)abilityEhp + 1.0);
			double newDamageRaw = DamageCalculator.SolveAttackDamageForIncomingDamage(neededDamage, defense, incomingPercent);
			double playerDpsBeforeCrit = ((double)abilityDps + 1.0) * dps;
			double critDps = critDamage * critChance * playerDpsBeforeCrit;
			double playerDps = (1.0 - critChance) * playerDpsBeforeCrit + critDps;
			double newHealthRaw = ((playerDps * (double)c.AverageFightTime) / (double)c.Count) * (double)(1f - usedResist);
			double newDamage = Math.Round(Math.Max(10.0, newDamageRaw) / 10.0) * 10.0;
			double newHealth = Math.Round(newHealthRaw);
			return new SpawnerBakeReportData
			{
				Snapshot = snapshot,
				Key = c.Key,
				Name = spawner.name,
				LocationNumber = c.LocationNumber,
				TimeChunkIndex = snapshot.TimeChunkIndex,
				Playtime = snapshot.Playtime,
				OptimumTime = optimumTime,
				ReCalcOptimumTime = reCalcOptimumTime,
				PrevHealth = previousHealth.BaseValue,
				PrevDamage = previousDamage.BaseValue,
				Count = c.Count,
				AverageFightTime = c.AverageFightTime,
				ArmorType = chosenArmor.Type,
				ArmorResistRaw = chosenArmor.Resist,
				ArmorResistUsed = usedResist,
				PreferredWeaponType = location.PreferredWeaponType,
				CritChanceRaw = rawCritChance,
				CritDamageRaw = rawCritDamage,
				CritChance = critChance,
				CritDamage = critDamage,
				DpsByArmorType = dps,
				PlayerDpsBeforeCrit = playerDpsBeforeCrit,
				CritDps = critDps,
				PlayerDps = playerDps,
				DefenseFromSnapshot = defense,
				Defense = defense,
				HealthRegenFromStats = healthRegen,
				FlatRegenFromStats = flatRegen,
				BattleRegen = battleRegen,
				PlayerEvasion = evasion,
				PlayerBlockChanceRaw = rawBlockChance,
				PlayerBlockAmountRaw = rawBlockAmount,
				PlayerBlockChance = blockChance,
				PlayerBlockAmount = blockAmount,
				BlockDamageMultiplier = blockMultiplier,
				EnemiesCount = enemies,
				DifficultyBeforeValidation = rawDifficulty,
				DifficultyUsed = difficulty,
				NeedTotalHpBaseHealth = baseHealth,
				NeedTotalHp = neededHealth,
				NeedDps = neededDps,
				NeedDpsFromOneUnit = neededDpsPerUnit,
				AttackCooldownBase = cooldown,
				HitFrequency = frequency,
				NeedDamageFromOneUnit = neededDamage,
				IncomingDamagePercentMultiplier = incomingPercent,
				PlayerAbilityDpsMultiplier = (double)abilityDps + 1.0,
				PlayerAblityEhpMultiplier = (double)abilityEhp + 1.0,
				NewDamageRaw = newDamageRaw,
				NewDamage = newDamage,
				NewHealthRaw = newHealthRaw,
				NewHealth = newHealth,
				ArmorCandidateRows = rows
			};
		}

		private float GetReCalcOptimumTime(EnemySpawner spawner)
		{
			var c = spawner.Config;
			if (c.LocationNumber >= EarlySpawnerOptimumTimeOffsetStartLocation && c.RelativeOptimumTime < EarlySpawnerOptimumTimeOffsetRelativePart)
			{
				float delta = (CombatBalanceConfig.IslandTargetDays[c.LocationNumber - 1] - CombatBalanceConfig.IslandTargetDays[c.LocationNumber - 2]) * (EarlySpawnerOptimumTimeOffsetRelativePart - c.RelativeOptimumTime) * EarlySpawnerOptimumTimeOffsetMultiplier;
				if (!c.IsDays)
					delta *= 60f;
				return c.OptimumTime - delta;
			}

			return c.OptimumTime;
		}

		private SimulationSnapshot GetSnapshotForBake(float playTime, bool isDays)
		{
			if (!isDays)
				return SimulationData.GetByPlayTime(playTime);
			var snapshots = TimeChunkSnapshots != null && TimeChunkSnapshots.Count > 0 ? TimeChunkSnapshots : SimulationData.TimeChunkSnapshots;
			return SimulationData.GetByTimeChunkIndex(playTime, snapshots);
		}
	}
}
