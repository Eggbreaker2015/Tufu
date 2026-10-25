using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Balance;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Enemy;
using Codebase.Gates;
using Codebase.Location;
using Codebase.Meta;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Player.Systems;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	public class SimulationSystem : SystemBase
	{
		private struct FastTickChunkStatReward
		{
			public string Name;

			public StatType StatType;

			public ModifierType ModifierType;

			public ModifierSource Source;

			public float Duration;

			public double Value;
		}

		[Inject]
		public PlayerModel playerModel;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private GatesModel gatesModel;

		[Inject]
		private ABTestService abTestService;

		[Inject]
		private CombatBalanceConfig combatBalanceConfig;

		[Inject]
		private PlayerRewardSystem playerRewardSystem;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private AmuletConfig amuletConfig;

		[Inject]
		private AmuletUpgradeConfig amuletUpgradeConfig;

		[Inject]
		private AmuletUpgradesModel amuletUpgradesModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private LocationsConfig locationsConfig;

		[Inject]
		private SimulationConfig config;

		private Vector3 currentPosition;

		private EnemySpawner currentSpawner;

		private EnemySpawner currentBoss;

		private double totalFightTime;

		private double totalMoveTime;

		private int countFight;

		private int countNeighbFight;

		private int countFightWithStrong;

		private int countRespawn;

		private List<ItemModel> equippedItems;

		private List<SimulationStatData> currentStatsData;

		private List<SimulationDpsData> currentDpsData;

		private List<SimulationDpsData> currentDefenseData;

		private List<Canvas> canvases;

		private bool uiDisabled;

		private Dictionary<StatType, float> statsBenefit;

		private List<StatType> statsBenefitKeys;

		private Dictionary<ItemModel, float> itemBenefit;

		private Dictionary<WeaponType, SimulationDpsData> dpsDataCache;

		public Dictionary<EnemySpawner, SpawnerData> spawnerData;

		private Dictionary<(EnemySpawner A, EnemySpawner B), List<EnemySpawner>> PathSpawners;

		private Dictionary<int, float> enemyHpDamageMultCache;

		private Dictionary<int, LocationConfig> locationByNumberCache;

		private Dictionary<WeaponType, SimulationDpsData> defenseDataCache;

		private double cachedDodgeChance;

		private double cachedBlockDamageMultiplier;

		private double cachedGlobalDefenceMultiplier;

		private double cachedCritMultiplier;

		private double cachedGlobalAttackMultiplier;

		private List<EnemySpawner> sortedSpawners;

		private List<EnemySpawner> statBenefitCandidates;

		private Comparison<EnemySpawner> optimumTimeComparer;

		public Dictionary<StatType, double> RPMCalculatedValue;

		public Dictionary<string, double> SoulRPMCalculatedValue;

		private Dictionary<EnemySpawner, int> spawnerChargesUsage;

		private int timeChunkIndex;

		private bool wasFastTickActive;

		private int lifeCycleIndex;

		private float lifeCyclePeriodEnd;

		private float lastAdsTime;

		private StatType? lastRewardStat;

		private float maxOptimumTime;

		private float autoGamblingOptimumTime;

		private int autoBalanceAmuletTargetIndex;

		private bool isSimulationPrepared;

		private double autoBalanceAmuletPreviousExperience;

		private readonly Dictionary<string, double> autoBalanceAmuletLevelSoulRewards = new Dictionary<string, double>();

		private AmuletSimulationMultipliers amuletSimulationMultipliers;

		private int currentLocationNumber = 1;

		private StringBuilder debugStringBuilder;

		private const string SimulationInterpolationStatModifierName = "__simulationInterpolationStatModifier";

		private const string SimulationStatsFromFeaturesFlatModifierName = "__simulationStatsFromFeaturesFlatModifier";

		private static readonly HashSet<StatType> MainInterpolationStats = new HashSet<StatType> { StatType.StabDamageFlat, StatType.CutDamageFlat, StatType.BuckshotDamageFlat, StatType.StabDefenceFlat, StatType.CutDefenceFlat, StatType.BuckshotDefenceFlat, StatType.Health, StatType.HealthRegen, StatType.LifestealFlat };

		private static readonly HashSet<StatType> CombatFlatInterpolationStats = new HashSet<StatType> { StatType.StabDamageFlat, StatType.CutDamageFlat, StatType.BuckshotDamageFlat, StatType.StabDefenceFlat, StatType.CutDefenceFlat, StatType.BuckshotDefenceFlat };

		private static readonly HashSet<StatType> DirectInterpolationStats = new HashSet<StatType> { StatType.Health, StatType.HealthRegen, StatType.LifestealFlat };

		private readonly Dictionary<StatType, FastTickChunkStatReward> fastTickChunkStatRewards = new Dictionary<StatType, FastTickChunkStatReward>();

		private readonly Dictionary<ItemConfig, double> fastTickChunkItemRewards = new Dictionary<ItemConfig, double>();

		private readonly Dictionary<string, double> fastTickChunkSoulRewards = new Dictionary<string, double>();

		private readonly HashSet<EnemySpawner> fastTickChunkGateRewardSpawners = new HashSet<EnemySpawner>();

		private int fastTickChunkKillCount;

		private static readonly string[] AutoBalanceAmuletCostSoulKeys = new string[] { "common", "large", "elite" };

		public static SimulationSystem Instance { get; private set; }

		public bool UiDisabled => this.uiDisabled;

		public bool IsRunning => config.IsRunning;

		public SimulationData SimulationData => config.SimulationData;

		[StaticReset]
		private static void ResetStaticState()
		{
// Original ELF VA 0x13e12fc.
            Instance = null;
		}

		private double GetSimulationEnemyMultiplier()
		{
// Original ELF VA 0x13e13b0.
            return config.IsRunning ? (double)config.EnemyHpDamageMultiplier : 1.0;
		}

		private (double, double, double, double) GetRPMConfig()
		{
// Original ELF VA 0x13e13dc: exact original tuple constants/order.
            return (70.0, 50.0, 2.0, 2.0);
		}

		private (double, double) GetFastRPMConfig()
		{
// Original ELF VA 0x13e1468; authored float parameters widen separately.
            var data = abTestService.GetFastRPMParams();
            return ((double)data.fightTimeSeconds, (double)data.maxHealthLossMultiplier);
		}

		public override void OnAwake()
		{
// Original ELF VA 0x13e14f8. Ordinary runtime does not enable the offline bot.
            Instance = this;
            equippedItems = new List<ItemModel>();
            currentStatsData = new List<SimulationStatData>();
            currentDpsData = new List<SimulationDpsData>();
            currentDefenseData = new List<SimulationDpsData>();
            spawnerChargesUsage = new Dictionary<EnemySpawner, int>();
            timeChunkIndex = 0;
            debugStringBuilder = new StringBuilder();
            currentPosition = Vector3.zero;
            currentSpawner = null;
            lastAdsTime = 0f;
            lastRewardStat = null;
            totalMoveTime = totalFightTime = 0.0;
            countFight = countNeighbFight = countFightWithStrong = countRespawn = 0;
            config.IsRunning = config.AutoGambling;
            config.Snapshots = new List<SimulationSnapshot>();
            config.TimeChunkSnapshots = new List<SimulationSnapshot>();
            config.Log = string.Empty;
            config.CurrentPlaytime = config.CurrentTotalPlaytime = 0f;
            if (config.AutoGambling) combatBalanceConfig.ResetItemCostAndValues();
            maxOptimumTime = 0f;
            lifeCycleIndex = 0;
            lifeCyclePeriodEnd = config.LifeCycleInMinutes != null && config.LifeCycleInMinutes.Length != 0
                ? config.LifeCycleInMinutes[0] : 0f;
            uiDisabled = false;
            config.RewardList = new List<RewardEntry>();
            config.curveBossDifficult.keys = Array.Empty<Keyframe>();
            canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).ToList();
            statsBenefit = new Dictionary<StatType, float>();
            foreach (StatType type in Enum.GetValues(typeof(StatType))) statsBenefit[type] = 1f;
            statsBenefitKeys = new List<StatType>(statsBenefit.Keys);
            itemBenefit = new Dictionary<ItemModel, float>();
            spawnerData = new Dictionary<EnemySpawner, SpawnerData>();
            PathSpawners = new Dictionary<(EnemySpawner A, EnemySpawner B), List<EnemySpawner>>();
            RPMCalculatedValue = new Dictionary<StatType, double>();
            SoulRPMCalculatedValue = new Dictionary<string, double>();
            dpsDataCache = new Dictionary<WeaponType, SimulationDpsData>();
            enemyHpDamageMultCache = new Dictionary<int, float>();
            locationByNumberCache = new Dictionary<int, LocationConfig>();
            defenseDataCache = new Dictionary<WeaponType, SimulationDpsData>();
            statBenefitCandidates = new List<EnemySpawner>();
            optimumTimeComparer = (a, b) => a.Config.OptimumTime.CompareTo(b.Config.OptimumTime);
            sortedSpawners = enemiesModel.Spawners.OrderBy(spawner => spawner.Config.LocationNumber).ToList();
            autoGamblingOptimumTime = enemiesModel.Spawners.Max(GetSpawnerOptimumTimingForCurrentMode);
            foreach (StatType type in Enum.GetValues(typeof(StatType))) RPMCalculatedValue[type] = 0.0;
            foreach (var soul in amuletModel.SoulDefs) SoulRPMCalculatedValue[soul.Key] = 0.0;
            if (config.IsRunning) PrepareSimulationRun();
		}

		public override void OnUpdate(float deltaTime)
		{
// Original ELF VA 0x13e276c. deltaTime is deliberately unused by the authored bot.
            if (config.handTick)
            {
                config.handTick = false;
                PrepareSimulationRun();
                ExecuteTick();
            }
            if (!config.IsRunning) return;
            PrepareSimulationRun();
            for (int i = 0; i < config.SimulationSpeed; i++) ExecuteTick();
            if (uiDisabled == config.DisableUi) return;
            foreach (var canvas in canvases)
                if (canvas != null) canvas.gameObject.SetActive(!config.DisableUi);
            uiDisabled = config.DisableUi;
		}

		private void ExecuteTick()
		{
		}

		private void PrepareSimulationRun()
		{
		}

		private void CheckSimulationStopConditions()
		{
		}

		public void CalculateSimulationData()
		{
// Original bounded 52-byte body, ELF VA 0x13e4444.
            CalculateDpsData();
            UpdateDpsDataCache();
            CalculateDefenseData();
            RebuildPerFrameCaches();
            CalculateSpawnerData(true);
		}

		private void RebuildPerFrameCaches()
		{
// Original ELF VA 0x13e5bc0: location cache is populated once, not cleared each frame.
            enemyHpDamageMultCache.Clear();
            if (locationByNumberCache.Count == 0)
                foreach (var location in locationsConfig.Locations) locationByNumberCache[location.Number] = location;
            defenseDataCache.Clear();
            foreach (var data in currentDefenseData) defenseDataCache[data.WeaponType] = data;
            cachedDodgeChance = GetForecastStatSpecificValue(StatType.Evasion);
            double blockChance = GetForecastStatSpecificValue(StatType.BlockChance);
            double blockAmount = GetForecastStatSpecificValue(StatType.BlockAmount);
            cachedBlockDamageMultiplier = 1.0 - blockChance * blockAmount;
            cachedGlobalDefenceMultiplier = 1.0 / (GetForecastStatSpecificValue(StatType.GlobalDefence) * 0.01 + 1.0);
            double critChance = GetForecastStatSpecificValue(StatType.CritChance);
            double critMultiplier = GetForecastStatSpecificValue(StatType.CritDamagePercent);
            cachedCritMultiplier = critChance * (critMultiplier + -1.0) + 1.0;
            cachedGlobalAttackMultiplier = GetForecastStatSpecificValue(StatType.GlobalAttack) * 0.01 + 1.0;
		}

		private float GetCachedEnemyHpDamageMultiplier(int locationNumber)
		{
// Original ELF VA 0x13e68ac.
            if (enemyHpDamageMultCache.TryGetValue(locationNumber, out float value)) return value;
            value = abTestService.GetEnemyHpDamageMultiplier(locationNumber);
            enemyHpDamageMultCache[locationNumber] = value;
            return value;
		}

		private LocationConfig GetCachedLocation(int locationNumber)
		{
// Original ELF VA 0x13e6980; missing configured location remains First's exception.
            if (locationByNumberCache.TryGetValue(locationNumber, out var location)) return location;
            location = locationsConfig.Locations.First(entry => entry.Number == locationNumber);
            locationByNumberCache[locationNumber] = location;
            return location;
		}

		public void CalculateRPMValue()
		{
// Original ELF VA 0x13e6adc.
            CalculateSimulationData();
            CalculateSoulRPMValues();
            foreach (StatType type in Enum.GetValues(typeof(StatType))) CalculateRPMValue(type);
		}

		public void CalculateRPMValueTimeChunk()
		{
// Original ELF VA 0x13e7a00.
            CalculateSimulationData();
            CalculateSoulRPMValues();
            foreach (StatType type in Enum.GetValues(typeof(StatType))) CalculateRPMValueTimeChunk(type);
		}

		public List<SimulationDpsData> GetCurrentDpsData()
		{
// Original ELF VA 0x13e8334.
            return currentDpsData.ToList();
		}

		public List<SimulationDpsData> GetCurrentDefenseData()
		{
// Original ELF VA 0x13e837c.
            return currentDefenseData.ToList();
		}

		public void CalculateRPMValue(StatType statType)
		{
// Original ELF VA 0x13e71d4, closures 0x13f46e4/47dc/47fc/40f4.
            if (!abTestService.UseOldRpmAlgorithm()) { CalculateRPMValueTimeChunk(statType); return; }
            var candidates = spawnerData.Values.Where(data => data.spawner.Config.StatRewards != null && data.playerRPS > 0.0
                && data.spawner.Config.StatRewards.Any(reward => reward.StatType == statType)).ToList();
            if (candidates.Count == 0) return;
            var rpm = GetRPMConfig();
            double totalTime = 0.0;
            double totalReward = 0.0;
            int count = 0;
            foreach (var data in candidates.OrderByDescending(entry => entry.playerRPS))
            {
                var reward = data.spawner.Config.StatRewards.First(entry => entry.StatType == statType);
                double speed = GetForecastStatSpecificValue(StatType.MovementSpeed);
                float cooldown = data.spawner.BaseCooldown;
                int units = data.spawner.Config.Count;
                double time = data.farmedTime + rpm.Item4 * (double)count / speed;
                double fights = Math.Min(1000.0 / (double)(cooldown * (float)units), (1000.0 - totalTime) / time);
                totalTime += time * fights;
                totalReward += fights * (double)reward.Value * (double)data.spawner.Config.Count;
                count++;
                if (totalTime >= 999.9) break;
            }
            double value = Math.Min(1.0, 1000.0 / totalTime) * (totalReward / Math.Max(1000.0, totalTime)) * 60.0;
            if (value > 0.0) value = Math.Max(0.5, value);
            if (RPMCalculatedValue.ContainsKey(statType)) value = Math.Max(value, RPMCalculatedValue[statType]);
            RPMCalculatedValue[statType] = value;
		}

		public void CalculateRPMValueTimeChunk(StatType statType)
		{
// Original ELF VA 0x13e7cf4: original location multipliers and FP operation order.
            var parameters = GetFastRPMConfig();
            var locationMultipliers = new double[] { 2.0, 2.0 };
            var candidates = spawnerData.Values.Where(data => data.spawner.Config.StatRewards != null
                && data.spawner.Config.StatRewards.Any(reward => reward.StatType == statType)).ToList();
            if (candidates.Count == 0) return;
            double health = GetForecastStatValue(StatType.Health);
            double total = 0.0;
            foreach (var data in candidates)
            {
                double loss = -data.healthDelta;
                if (parameters.Item2 * health < loss) continue;
                var reward = data.spawner.Config.StatRewards.First(entry => entry.StatType == statType);
                double fightTime = data.fightTime;
                double healthMultiplier = 1.0;
                if (health < loss) healthMultiplier = Math.Sqrt(health / loss);
                double locationMultiplier = 1.0;
                var def = data.spawner.Config;
                int index = unchecked(def.LocationNumber - 1);
                if (!def.IsEliteBoss && index >= 0 && index < locationMultipliers.Length) locationMultiplier = locationMultipliers[index];
                int charges = GetSpawnerChargesLimit(data.spawner);
                double contribution = parameters.Item1 / (parameters.Item1 + fightTime);
                contribution *= (double)reward.Value;
                contribution *= (double)charges;
                contribution *= locationMultiplier;
                contribution *= healthMultiplier;
                total += contribution;
            }
            double value = total / 60.0;
            if (value > 0.0) value = Math.Max(0.5, value);
            value *= (double)abTestService.GetRpmStatValueMultiplier(statType);
            if (RPMCalculatedValue.ContainsKey(statType)) value = Math.Max(value, RPMCalculatedValue[statType]);
            RPMCalculatedValue[statType] = value;
		}

		public void CalculateSoulRPMValues()
		{
// Original ELF VA 0x13e6dd0: distinct configured nonblank keys, preserving dictionary order.
            var keys = new HashSet<string>();
            foreach (var data in spawnerData.Values)
                if (!string.IsNullOrWhiteSpace(data.spawner.Config.SoulReward)) keys.Add(data.spawner.Config.SoulReward);
            foreach (string key in keys) CalculateSoulRPMValue(key);
		}

		public void CalculateSoulRPMValue(string soulKey)
		{
// Original ELF VA 0x13e8424; soul rate uses charges and original Meta calculator.
            double health = GetForecastStatValue(StatType.Health);
            double total = 0.0;
            foreach (var data in spawnerData.Values)
            {
                if (health < -data.healthDelta) continue;
                if (data.spawner.Config.SoulReward != soulKey) continue;
                double fightTime = data.fightTime;
                int charges = GetSpawnerChargesLimit(data.spawner);
                total += (10.0 / (fightTime + 10.0)) * (double)charges;
            }
            SetSoulRPMValue(soulKey, global::Codebase.Meta.SoulDropAmountCalculator.CalculateForKillCount(soulKey, total / 60.0, metaStatsModel));
		}

		private void SetSoulRPMValue(string soulKey, double value)
		{
// Original ELF VA 0x13e86b8; positive-only floor and monotonic existing dictionary value.
            if (value > 0.0) value = Math.Max(0.03, value);
            if (SoulRPMCalculatedValue.ContainsKey(soulKey))
                value = Math.Max(value, SoulRPMCalculatedValue[soulKey]);
            SoulRPMCalculatedValue[soulKey] = value;
		}

		public StatType CalculateBestRewardStat()
		{
			return StatType.MovementSpeed;
		}

		public EnemySpawner GetBestSpawnerForInfo()
		{
			return null;
		}

		private void Tick()
		{
		}

		private void FastTick()
		{
		}

		public float GetBalanceTimingMinutes()
		{
// Original ELF VA 0x13ebe60: original float FRINTM, not an integer cast.
            return config.UseSpawnerCharges
                ? Mathf.Floor(config.CurrentTotalPlaytime / config.SpawnerChargesChunkMinutes)
                : config.CurrentPlaytime / 60f;
		}

		public float GetNextLocationInterpolationTiming(int locationNumber)
		{
			return 0f;
		}

		public float ConvertIslandTimingToDays(float islandTiming)
		{
			return 0f;
		}

		private float GetSpawnerOptimumTimingForCurrentMode(EnemySpawner spawner)
		{
// Original ELF VA 0x13ed83c.
            if (config.UseSpawnerCharges && !spawner.Config.IsDays)
                return spawner.Config.OptimumTime / config.TimeChunkDurationMinutes;
            return spawner.Config.OptimumTime;
		}

		private (double, double, string) GetUnlockTimingLogData(EnemySpawner spawner)
		{
			return default;
		}

		public int RollFractionalCount(double value)
		{
			return 0;
		}

		private static double GetChainedItemBaseCount(int baseItemsCount, float value)
		{
			return 0.0;
		}

		private void ProcessAFK(float minutes)
		{
		}

		private void NextLifeCycle()
		{
		}

		private void CheckLifeCycle()
		{
		}

		private void ProcessSimulateAds()
		{
		}

		private void ProcessCrystalSimulationChunk()
		{
		}

		private void UpgradeCrystalsGreedy()
		{
		}

		private void ResetCooldowns()
		{
		}

		private void UpdateSpawnerChargesChunk()
		{
		}

		private void RecalculateChunkRpmValues()
		{
		}

		private void AccumulateFastTickSpawnerRewards(EnemySpawner spawner, int fightsCount)
		{
		}

		private void FlushFastTickChunkRewards()
		{
		}

		private void ResetAmuletAutoBalance()
		{
		}

		private void ProcessAmuletAutoBalanceTargets(int chunkIndex)
		{
		}

		private int GetSpawnerChargesLimit(EnemySpawner spawner)
		{
// Original ELF VA 0x13e83ec.
            return spawner.Config.DisableAfterFirstClear ? spawner.Config.Count : spawner.ChargesTotal;
		}

		private int GetSpawnerChargesPerClear(EnemySpawner spawner)
		{
			return 0;
		}

		private void CalculateSpawnerPaths(EnemySpawner A, EnemySpawner B)
		{
		}

		private void CalculateSpawnerData(bool forGPMBot = false)
		{
// Original ELF VA 0x13e6074. Preserve float32 count factor and widened float literals.
            var rpm = GetRPMConfig();
            spawnerData.Clear();
            int unlockedLocation = playerModel.LastUnlockedLocation.Number;
            double enemyMultiplier = GetSimulationEnemyMultiplier();
            double health = GetForecastStatValue(StatType.Health);
            double speed = GetForecastStatSpecificValue(StatType.MovementSpeed);
            double missingHealth = Math.Max(0.0, health - playerModel.Unit.CurrentHealth);
            double moveTime = rpm.Item1 / speed;
            foreach (var spawner in sortedSpawners)
            {
                var def = spawner.Config;
                if (def.IsEvent || def.IsVip) continue;
                if (!forGPMBot)
                {
                    if (!spawner.AnyoneAlive) continue;
                    if (!(spawner.Cooldown <= 0f) && config.IsRunning) continue;
                    if (config.UseSpawnerCharges)
                    {
                        spawnerChargesUsage.TryGetValue(spawner, out int used);
                        if (used >= GetSpawnerChargesLimit(spawner)) continue;
                    }
                    if (config.IsCapOptimumTime)
                    {
                        if (config.UseSpawnerCharges && def.IsDays)
                        { if (def.OptimumTime > (float)timeChunkIndex) continue; }
                        else if (def.OptimumTime > config.CurrentPlaytime) continue;
                    }
                }
                else if (def.DisableAfterFirstClear) continue;
                if (def.LocationNumber > unlockedLocation) continue;
                float abMultiplier = GetCachedEnemyHpDamageMultiplier(def.LocationNumber);
                var data = new SpawnerData(playerModel, config, this) { spawner = spawner };
                data.playerDPS = GetDpsAgainstSpawner(spawner, true) + (double)0.1f;
                float countFactor = Math.Min((float)unchecked(def.Count + 1) / 2f, 4f);
                data.enemyDPS = ((GetSpawnerDps(spawner, true) + (double)0.1f) * (double)countFactor) / GetPlayerAblityEhpMultiplier();
                double baseHealth = def.Stats.FirstOrDefault(stat => stat.Type == StatType.Health).BaseValue;
                data.fightTime = enemyMultiplier * (baseHealth * (double)def.Count) * (double)abMultiplier / data.playerDPS;
                double delta = (GetBattleRegen((double)def.Difficulty) - data.enemyDPS) * data.fightTime;
                data.healthDelta = delta > missingHealth ? missingHealth : delta;
                data.rewardValue = CalculateSpawnerRewardValue(spawner);
                double regenTime = rpm.Item2 * Math.Max(0.0, -data.healthDelta / Math.Max(1.0, health));
                data.farmedTime = regenTime + (moveTime + rpm.Item3 * data.fightTime);
                data.avMoveTime = moveTime;
                data.avRegenTime = regenTime;
                data.playerRPS = GetResourceValue(spawner) / data.farmedTime;
                if (health * 0.5 < -data.healthDelta) data.playerRPS *= (double)0.2f;
                if (data.fightTime < 300.0) spawnerData[spawner] = data;
            }
		}

		private void ProcessDeath()
		{
		}

		private double GetRegen()
		{
// Original ELF VA 0x13efe28.
            return GetForecastStatValue(StatType.HealthRegen);
		}

		private double GetBattleRegen(double difficulty)
		{
// Original ELF VA 0x13efd54.
            return global::Codebase.Services.Damage.DamageCalculator.GetBattleRegen(
                GetForecastStatValue(StatType.HealthRegen), GetForecastStatValue(StatType.LifestealFlat), difficulty);
		}

		private void ApplyRegen(double seconds)
		{
		}

		private double GetFightEfficiency(EnemySpawner spawner)
		{
			return 0.0;
		}

		private double GetTotalDPS()
		{
			return 0.0;
		}

		private double GetTotalArmor()
		{
			return 0.0;
		}

		private double CalculateFarmPotencial(EnemySpawner spawner, double totalPath, double totalDamage)
		{
			return 0.0;
		}

		private void CalculateStatBenefit()
		{
		}

		private (Stat, StatModifier) ModifyFromItem(ItemModel item)
		{
			return default;
		}

		private void DrawSquare(Vector3 pos, float size, Color color)
		{
		}

		private SpawnerData FindBestSpawner()
		{
			return null;
		}

		private void SimulateFightWithSpawner(SpawnerData bestSpawner)
		{
		}

		private bool SimulateFightWithSpawnerFast(SpawnerData bestSpawner, int fightsCount)
		{
			return false;
		}

		private void UnlockSimulationWeaponSlot(int locationNumber)
		{
		}

		private double CalcDifficultSpawner(EnemySpawner spawner, float secondsRegen = 0f)
		{
			return 0.0;
		}

		public int GetForecastUnlockedLocationNumber()
		{
			return 0;
		}

		public double GetResourceValue(EnemySpawner spawner)
		{
// Original ELF VA 0x13efd98: first configured reward only.
            var rewards = spawner.Config.StatRewards;
            return rewards != null && rewards.Count != 0 ? (double)rewards[0].Value : 0.0;
		}

		public double CalculateSpawnerRewardValue(EnemySpawner spawner, StatType? statType = null)
		{
// Original ELF VA 0x13e0cb8; authored item benefit fallback is intentionally retained.
            var rewards = spawner.Config.StatRewards;
            if (statType.HasValue)
            {
                var reward = rewards == null ? null : rewards.FirstOrDefault(entry => entry.StatType == statType);
                return reward == null ? 0.0 : (double)reward.Value;
            }
            if (spawner == currentBoss) return 1000000.0;
            double value = 0.0;
            if (rewards != null)
                foreach (var reward in rewards)
                    if (statsBenefit.TryGetValue(reward.StatType, out float benefit)) value += (double)benefit * (double)reward.Value;
            if (spawner.Config.ItemReward != null)
            {
                var item = playerModel.Items.FirstOrDefault(entry => entry.Config == spawner.Config.ItemReward);
                double itemValue = 10000.0;
                if (item != null && itemBenefit.TryGetValue(item, out float benefit)) itemValue = value + (double)benefit;
                value = itemValue;
            }
            return value * (double)spawner.Config.Count;
		}

		public bool TryGetSpawnerPriority(EnemySpawner spawner, out string priorityInfo)
		{
			priorityInfo = null;
			return false;
		}

		private void ProcessInventory()
		{
		}

		private void TryApplyArmorBaseAutoBalance(ItemModel item)
		{
		}

		private void ApplyAutoBalanceForEvolution(ItemModel item)
		{
		}

		private int GetSpawnerItemRewardCopies(EnemySpawner spawner)
		{
			return 0;
		}

		private float GetItemEmissionCoefficient(float timing, ItemConfig item)
		{
			return 0f;
		}

		private bool IsPreferredItemEmissionType(ItemConfig item)
		{
			return false;
		}

		private void MakeSnapshot(bool force = false)
		{
		}

		private double GetDpsAgainstSpawner(EnemySpawner spawner, bool isFast)
		{
// Original ELF VA 0x13ef248: normal shop uses the fast cached branch.
            if (isFast)
            {
                Armor armor = GetBestArmorAgainstDps(spawner);
                double value = dpsDataCache.TryGetValue(armor.Type, out var data) ? data.Value : 0.0;
                return GetPlayerAbilityDpsMultiplier() * cachedGlobalAttackMultiplier * cachedCritMultiplier * value * (double)(1f - armor.Resist);
            }
            if (currentLocationNumber != spawner.Config.LocationNumber)
            {
                currentLocationNumber = spawner.Config.LocationNumber;
                ProcessInventory();
            }
            double total = 0.0;
            foreach (var item in equippedItems.Where(entry => !entry.IsArmor))
            {
                var hit = new global::Codebase.Unit.Events.WeaponHitEvent {
                    Source = playerModel.Unit, Target = spawner.FirstUnit, Weapon = item,
                    DamageType = item.Config.DamageType,
                    Armor = spawner.Config.ArmorConfig.Armors.First(armor => armor.Type == item.Config.DamageType) };
                double damage = global::Codebase.Services.Damage.DamageCalculator.Calculate(hit, true, 1.0).Damage;
                total += damage * (double)item.Config.SimulationDpsWeaponMultiplier / (double)item.Config.AttackCooldown;
            }
            return total * GetPlayerAbilityDpsMultiplier();
		}

		private Armor GetBestArmorAgainstDps(EnemySpawner spawner)
		{
// Original ELF VA 0x13f2cf8; first ties and original fallback are preserved.
            double best = double.MinValue;
            Armor selected = null;
            foreach (var armor in spawner.Config.ArmorConfig.Armors)
            {
                if (dpsDataCache.TryGetValue(armor.Type, out var data))
                {
                    double value = data.Value * (double)(1f - armor.Resist);
                    if (value > best) { best = value; selected = armor; }
                }
            }
            return selected ?? spawner.Config.ArmorConfig.Armors.FirstOrDefault();
		}

		private void UpdateDpsDataCache()
		{
// Original ELF VA 0x13e4f24; original method does not clear the dictionary first.
            foreach (var data in currentDpsData) dpsDataCache[data.WeaponType] = data;
		}

		private double GetPlayerAbilityDpsMultiplier()
		{
// Original ELF VA 0x13f2f98; retain original Rare interpolation selection.
            return (double)combatBalanceConfig.GetInterpolationCoefficient(
                GetBalanceTimingMinutes(), BalanceInterpolationType.PlayerAbilityDPSMult, Rarity.Rare) + 1.0;
		}

		private double GetPlayerAblityEhpMultiplier()
		{
// Original ELF VA 0x13efd18.
            return (double)combatBalanceConfig.GetInterpolationCoefficient(
                GetBalanceTimingMinutes(), BalanceInterpolationType.PlayerAblityEHPMult, Rarity.Rare) + 1.0;
		}

		public float GetSimulationInterpolationCoefficient(float timing, BalanceInterpolationType type, Rarity rarity = Rarity.Rare)
		{
			return 0f;
		}

		private void UpdateInterpolationStatModifier()
		{
		}

		private double GetSimulationSnapshotStatValue(Stat stat)
		{
			return 0.0;
		}

		private bool UsesInterpolationStats(StatType statType)
		{
			return false;
		}

		private void CalculateDpsData()
		{
// Original ELF VA 0x13e4478 and closures 0x13f4634/465c/514c.
            currentDpsData.Clear();
            foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
            {
                equippedItems.Clear();
                var candidates = playerModel.Items.Where(item => !item.IsArmor)
                    .OrderByDescending(item => item.Config.DamageType == weaponType)
                    .ThenByDescending(item => item.DPS);
                double total = 0.0;
                foreach (var item in candidates)
                {
                    if (equippedItems.Count >= playerModel.UnlockedWeaponSlots) break;
                    equippedItems.Add(item);
                    double flat = GetForecastStatValue(item.Config.DamageStatTypeFlat);
                    double percent = GetForecastStatValue(item.Config.DamageStatTypePercent);
                    float coefficient = combatBalanceConfig.GetInterpolationCoefficient(
                        GetBalanceTimingMinutes(), BalanceInterpolationType.StatModifier, Rarity.Rare);
                    double dps = percent * (flat + item.Damage) * (double)coefficient / (double)item.Config.AttackCooldown;
                    if (item.Config.DamageType != weaponType) dps /= 10.0;
                    total += dps * (double)item.Config.SimulationDpsWeaponMultiplier;
                }
                currentDpsData.Add(new SimulationDpsData { WeaponType = weaponType, Value = total });
            }
		}

		private void CalculateDefenseData()
		{
// Original ELF VA 0x13e5104 and closures 0x13f4674/468c/5180.
            currentDefenseData.Clear();
            foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
            {
                equippedItems.Clear();
                var candidates = playerModel.Items.Where(item => item.IsArmor)
                    .Where(item => item.Config.DamageType == weaponType).OrderByDescending(item => item.Armor);
                double flat = GetForecastStatValue(global::Codebase.Services.Damage.DamageCalculator.WeaponTypeToDefence(weaponType, false));
                double percent = GetForecastStatValue(global::Codebase.Services.Damage.DamageCalculator.WeaponTypeToDefence(weaponType, true));
                double armor = 0.0;
                foreach (var item in candidates)
                {
                    if (equippedItems.Count >= playerModel.UnlockedArmorSlots) break;
                    equippedItems.Add(item);
                    armor += item.Armor;
                }
                float coefficient = combatBalanceConfig.GetInterpolationCoefficient(
                    GetBalanceTimingMinutes(), BalanceInterpolationType.StatModifier, Rarity.Rare);
                currentDefenseData.Add(new SimulationDpsData {
                    WeaponType = weaponType, Value = percent * (flat + armor) * (double)coefficient });
            }
		}

		private double GetSpawnerDps(EnemySpawner spawner, bool isFast)
		{
// Original ELF VA 0x13ef8d4; static spawners deal no modeled damage.
            if (spawner.Config.IsStatic) return 0.0;
            if (isFast)
            {
                var location = GetCachedLocation(spawner.Config.LocationNumber);
                double enemyMultiplier = spawner.Config.IsEvent ? 1.0 : (double)GetCachedEnemyHpDamageMultiplier(spawner.Config.LocationNumber);
                double baseDamage = spawner.Config.Stats.FirstOrDefault(stat => stat.Type == StatType.Damage).BaseValue;
                double defense = defenseDataCache.TryGetValue(location.PreferredWeaponType, out var data) ? data.Value : 0.0;
                double damage = global::Codebase.Services.Damage.DamageCalculator.ApplyDamageDefense(
                    GetSimulationEnemyMultiplier() * (baseDamage * enemyMultiplier), defense);
                double attackCooldown = spawner.Config.Stats.FirstOrDefault(stat => stat.Type == StatType.AttackCooldown).BaseValue;
                double dps = damage / attackCooldown;
                dps *= 1.0 - cachedDodgeChance;
                dps *= cachedBlockDamageMultiplier;
                dps *= cachedGlobalDefenceMultiplier;
                return dps;
            }
            double dodge = GetForecastStatSpecificValue(StatType.Evasion);
            double block = GetForecastStatSpecificValue(StatType.BlockChance);
            double blockAmount = GetForecastStatSpecificValue(StatType.BlockAmount);
            var hit = new global::Codebase.Unit.Events.WeaponHitEvent {
                Source = spawner.FirstUnit, Target = playerModel.Unit,
                DamageType = spawner.Config.WeaponType, Undodgeable = true };
            double result = global::Codebase.Services.Damage.DamageCalculator.Calculate(hit, true, 1.0).Damage;
            double total = (1.0 - dodge) * (result / (double)(float)spawner.FirstUnit.GetStatValue(StatType.AttackCooldown, true));
            total *= 1.0 - block * blockAmount;
            total *= GetSimulationEnemyMultiplier();
            total += 0.0;
            return double.IsNaN(total) ? 0.0 : total;
		}

		private double GetForecastStatValue(StatType type)
		{
// Original ELF VA 0x13e83c4.
            return playerModel.Unit.GetStatValue(type, true);
		}

		private double GetForecastStatSpecificValue(StatType type)
		{
// Original ELF VA 0x13e686c.
            return playerModel.Unit.GetStatSpecificValue(type, true);
		}

		private void LogSpawnerClear(EnemySpawner spawner, double healthDelta, double fightTime, double moveTime)
		{
		}
	}
}
