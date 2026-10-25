using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Codebase.Balance;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Services.Damage;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Simulation.Forecast
{
    public static class ForecastSnapshots
    {
        // Observable local compatibility boundary. Original snapshots remain the preferred source.
        public static bool LastBuildUsedCurrentPlayerFallback { get; private set; }
        public static string LastSnapshotSource { get; private set; }

        public static List<ForecastSnapshotExtended> Build(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats)
        {
            var output = new List<ForecastSnapshotExtended>();
            var routine = BuildCore(simulationSystem, input, presets, rewardAllowedStats, output, null, 1, false);
            while (routine.MoveNext()) { }
            return output;
        }
        public static IEnumerator BuildAsync(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, List<ForecastSnapshotExtended> output, Action<float> setProgress, int snapshotsPerFrame = 1)
        {
            return BuildCore(simulationSystem, input, presets, rewardAllowedStats, output, setProgress, snapshotsPerFrame, true);
        }

        // Original ARM64 ELF VA 0x13f9820. Time points follow configured boss count, not guessed extra stages.
        private static IEnumerator BuildCore(SimulationSystem simulationSystem, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, List<ForecastSnapshotExtended> output, Action<float> setProgress, int snapshotsPerFrame, bool yieldBetweenSteps)
        {
            output.Clear();
            LastBuildUsedCurrentPlayerFallback = false;
            LastSnapshotSource = "OriginalSimulationData";
            if (simulationSystem == null) yield break;
            snapshotsPerFrame = Math.Max(1, snapshotsPerFrame);
            var timePoints = BuildTimePoints(input);
            var bossMinutes = GetBossMinutes(input);
            var balance = GameContext.Get<CombatBalanceConfig>();
            for (int index = 0; index < timePoints.Count; index++)
            {
                float dayTiming = ConvertIslandTimingToDays(balance, timePoints[index]);
                float progress = (float)index / (timePoints.Count - 1);
                var snapshot = GetForecastSnapshot(simulationSystem, dayTiming, bossMinutes[index]);
                var rpm = snapshot.Stats.ToDictionary(stat => stat.Key, stat => stat.CurrentGpm);
                float multiplier = (float)Math.Max(0.0, (double)(snapshot.Playtime * input.EnemyStatsGrowthPerMinute) + 1.0);
                var reward = BuildReward(rpm, GetRewardMinutes(input, progress), rewardAllowedStats);
                // Original SimulationSystem wrapper 0x13ee9d0 only tail-calls this already restored config method.
                double abilityDps = (double)balance.GetInterpolationCoefficient(dayTiming, BalanceInterpolationType.PlayerAbilityDPSMult, Rarity.Rare) + 1.0;
                double abilityEhp = (double)balance.GetInterpolationCoefficient(dayTiming, BalanceInterpolationType.PlayerAblityEHPMult, Rarity.Rare) + 1.0;
                output.Add(new ForecastSnapshotExtended
                {
                    Snapshot = snapshot, AutoBalanceInput = input, EnemyStatsMultiplier = multiplier,
                    HasReward = reward.Item1, RewardStatType = reward.Item2, RewardMinutes = reward.Item3, RewardValue = reward.Item4,
                    Enemies = BuildAutoBalancedEnemies(snapshot, input, TunePresetsForBoss(presets, input, progress, index), multiplier, abilityDps, abilityEhp)
                });
                setProgress?.Invoke(Math.Min(1f, (float)(index + 1) / Math.Max(1, timePoints.Count)));
                if (yieldBetweenSteps && (index + 1) % snapshotsPerFrame == 0) yield return null;
            }
            setProgress?.Invoke(1f);
        }

        // Original 0x157ad44, inlined locally because the public SimulationSystem/config wrappers remain empty.
        internal static float ConvertIslandTimingToDays(CombatBalanceConfig balance, float islandTiming)
        {
            int island = Mathf.FloorToInt(islandTiming);
            float previous = island == 1 ? 0f : balance.IslandTargetDays[island - 2];
            float fraction = islandTiming - island;
            return fraction == 0f ? previous : previous + fraction * (balance.IslandTargetDays[island - 1] - previous);
        }
        private static SimulationSnapshot GetForecastSnapshot(SimulationSystem simulationSystem, float dayTiming, float eventMinute)
        {
            // Original 0x13f79d0: interpolate the original pre-baked simulation table, replace Playtime only.
            var data = simulationSystem.SimulationData;
            if (data != null && data.TimeChunkSnapshots != null && data.TimeChunkSnapshots.Count != 0)
            {
                var original = data.GetByTimeChunkIndex(dayTiming);
                if (original.Stats != null && original.DpsData != null && original.DefenseData != null)
                {
                    original.Playtime = eventMinute;
                    return original;
                }
            }
            // Explicit local learning fallback approved for missing future-table data: constant actual player capability.
            // No synthetic future growth, sample rewards, or profile mutations are introduced here.
            if (!LastBuildUsedCurrentPlayerFallback)
                Debug.Log("[LocalForecast] Original simulation table unavailable; using constant current-player capability.");
            LastBuildUsedCurrentPlayerFallback = true;
            LastSnapshotSource = "CurrentPlayerConstantFallback";
            simulationSystem.CalculateSimulationData();
            var player = simulationSystem.playerModel;
            var stats = player.Unit.Stats.Select(stat => new SimulationStatData
            {
                Key = stat.Type, Value = player.Unit.GetStatValue(stat.Type, true),
                TotalValue = player.Unit.GetStatValue(stat.Type, true),
                CurrentGpm = simulationSystem.RPMCalculatedValue.TryGetValue(stat.Type, out var rpm) ? rpm : 0.0
            }).ToList();
            return new SimulationSnapshot
            {
                Playtime = eventMinute, Location = player.BossProgressLocation.Number,
                Stats = stats, DpsData = new List<SimulationDpsData>(simulationSystem.GetCurrentDpsData()),
                DefenseData = new List<SimulationDpsData>(simulationSystem.GetCurrentDefenseData()),
                Items = player.Items.Select(item => new SimulationItemData { Key = item.Config.Key, Level = item.Level, Copies = item.Copies }).ToList()
            };
        }
        private static List<float> BuildTimePoints(ForecastAutoBalanceInput input) => Enumerable.Range(0, input.EventBossMinutes.Count).Select(index => input.ForecastLocationNumber + input.BossSnapshotTimingStep * index).ToList();
        private static List<float> GetBossMinutes(ForecastAutoBalanceInput input) => input.EventBossMinutes;

        // Original ARM64 ELF VA 0x13f5ee0 and tiny ToEnemyInfo/ToBossInfo bodies.
        public static ForecastEventPlan BuildEventPlan(IReadOnlyList<ForecastSnapshotExtended> snapshots, ForecastAutoBalanceInput input)
        {
            var bosses = new List<ForecastEventBossInfo>(input.EventBossMinutes.Count);
            for (int index = 0; index < input.EventBossMinutes.Count; index++)
                bosses.Add(ToBossInfo(input.EventBossMinutes[index], ForecastEnemyKind.EnemyBoss, snapshots[index].AutoBalanceInput, snapshots[index], FindEnemyOrDefault(snapshots[index], ForecastEnemyKind.EnemyBoss)));
            var first = snapshots[0];
            var last = snapshots[snapshots.Count - 1];
            return new ForecastEventPlan
            {
                Golem = ToEnemyInfo(0f, ForecastEnemyKind.EnemyGolem, first.AutoBalanceInput, FindEnemyOrDefault(first, ForecastEnemyKind.EnemyGolem)),
                Bosses = bosses,
                GrandBoss = ToEnemyInfo(Math.Max(0f, input.EventEndMinute), ForecastEnemyKind.EnemyGrandBoss, last.AutoBalanceInput, FindEnemyOrDefault(last, ForecastEnemyKind.EnemyGrandBoss))
            };
        }
        private static ForecastEnemyPresets TunePresetsForBoss(ForecastEnemyPresets presets, ForecastAutoBalanceInput input, float progress, int bossIndex)
        {
            float time = input.BossFightTimeSecondsMin + Mathf.Clamp01(progress) * (input.BossFightTimeSecondsMax - input.BossFightTimeSecondsMin);
            float difficulty = input.BossDifficultyMin + Mathf.Clamp01(bossIndex / 4f) * (input.BossDifficultyMax - input.BossDifficultyMin);
            if (time > 0f) presets.Boss.FightTimeSeconds = time;
            if (difficulty > 0f) presets.Boss.Difficulty = difficulty;
            return presets;
        }
        private static ForecastAutoBalancedEnemy FindEnemyOrDefault(ForecastSnapshotExtended snap, ForecastEnemyKind kind) => snap.Enemies == null ? default : snap.Enemies.FirstOrDefault(enemy => enemy.Kind == kind);
        private static ForecastEventEnemyInfo ToEnemyInfo(float minute, ForecastEnemyKind kind, ForecastAutoBalanceInput input, ForecastAutoBalancedEnemy enemy) => new ForecastEventEnemyInfo
        {
            Minute = minute, Kind = kind, Health = enemy.Health, Damage = enemy.Damage,
            AttackType = input.EnemyAttackType, ResistStabbing = input.ResistStabbing, ResistCutting = input.ResistCutting, ResistBuckshot = input.ResistBuckshot
        };
        private static ForecastEventBossInfo ToBossInfo(float minute, ForecastEnemyKind kind, ForecastAutoBalanceInput input, ForecastSnapshotExtended snap, ForecastAutoBalancedEnemy enemy) => new ForecastEventBossInfo
        {
            Enemy = ToEnemyInfo(minute, kind, input, enemy),
            Reward = new ForecastEventRewardInfo { HasReward = snap.HasReward, RewardStatType = snap.RewardStatType, RewardMinutes = snap.RewardMinutes, RewardValue = snap.RewardValue }
        };

        // Original 0x13f7da0 + local functions 0x13f87f0 and 0x13f8934.
        private static List<ForecastAutoBalancedEnemy> BuildAutoBalancedEnemies(SimulationSnapshot snapshot, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, float enemyStatsMultiplier, double playerAbilityDpsMultiplier, double playerAblityEhpMultiplier)
        {
            double Raw(StatType type) => snapshot.Stats.First(stat => stat.Key == type).TotalValue;
            double Specific(StatType type) => Stat.CalculateSpecificValue(type, Raw(type));
            double crit = Specific(StatType.CritChance) * (Specific(StatType.CritDamagePercent) - 1.0) + 1.0;
            double attack = Specific(StatType.GlobalAttack) * 0.01 + 1.0;
            double dodge = Specific(StatType.Evasion);
            double block = 1.0 - Specific(StatType.BlockChance) * Specific(StatType.BlockAmount);
            double globalDefence = 1.0 / (Specific(StatType.GlobalDefence) * 0.01 + 1.0);
            double PlayerDps(WeaponType type)
            {
                double resist = type == WeaponType.Stabbing ? input.ResistStabbing : type == WeaponType.Cutting ? input.ResistCutting : input.ResistBuckshot;
                resist = Math.Max(-0.9f, Math.Min(0.9f, (float)resist));
                return (1.0 - resist) * snapshot.DpsData.First(data => data.WeaponType == type).Value * crit * attack * playerAbilityDpsMultiplier;
            }
            double maxDps = Math.Max(PlayerDps(WeaponType.Stabbing), Math.Max(PlayerDps(WeaponType.Cutting), PlayerDps(WeaponType.Buckshot)));
            double defense = snapshot.DefenseData.FirstOrDefault(data => data.WeaponType == input.EnemyAttackType).Value;
            ForecastAutoBalancedEnemy BuildEnemy(ForecastEnemyKind kind, ForecastEnemySettings settings)
            {
                double multiplier = kind == ForecastEnemyKind.EnemyGolem ? 1.0 : enemyStatsMultiplier;
                double time = settings.FightTimeSeconds;
                double difficulty = settings.Difficulty;
                double target = ((Raw(StatType.Health) * difficulty / Math.Max(0.0001, time)) * (1.0 - dodge)) / block
                    + (difficulty + 1.0) * Raw(StatType.HealthRegen) + (difficulty + difficulty + 3.0) * Raw(StatType.LifestealFlat);
                double percent = globalDefence / playerAblityEhpMultiplier;
                // Compatibility validation avoids an unbounded native inverse solver on malformed table/config data.
                if (double.IsNaN(target) || double.IsInfinity(target) || percent <= 0 || double.IsNaN(percent) || double.IsInfinity(percent))
                    throw new InvalidOperationException("Invalid forecast combat inputs");
                double damage = DamageCalculator.SolveAttackDamageForIncomingDamage(target, defense, percent);
                damage = Math.Round(Math.Max(10.0, damage) / 10.0) * 10.0;
                damage = Math.Round(multiplier * damage / 10.0) * 10.0;
                if (kind != ForecastEnemyKind.EnemyGolem)
                    damage = Math.Round((damage / Math.Max(0.0001, GetAverageDamageRampMultiplier(time, input.DamageRampDelaySeconds, input.DamageRampPercentPerSecond))) / 10.0) * 10.0;
                return new ForecastAutoBalancedEnemy
                {
                    Kind = kind, FightTimeSeconds = settings.FightTimeSeconds, Difficulty = settings.Difficulty,
                    Health = Math.Round(multiplier * maxDps * time), Damage = Math.Max(10.0, damage)
                };
            }
            return new List<ForecastAutoBalancedEnemy> { BuildEnemy(ForecastEnemyKind.EnemyGolem, presets.Golem), BuildEnemy(ForecastEnemyKind.EnemyBoss, presets.Boss), BuildEnemy(ForecastEnemyKind.EnemyGrandBoss, presets.GrandBoss) };
        }
        private static double GetAverageDamageRampMultiplier(double fightTimeSeconds, double delaySeconds, double percentPerSecond)
        {
            if (fightTimeSeconds <= 0.0 || percentPerSecond <= 0.0) return 1.0;
            double delay = Math.Max(0.0, delaySeconds);
            return delay >= fightTimeSeconds ? 1.0 : 1.0 + (fightTimeSeconds - delay) * (fightTimeSeconds - delay) * percentPerSecond / (fightTimeSeconds + fightTimeSeconds);
        }
        private static (bool, StatType, float, double) BuildReward(IReadOnlyDictionary<StatType, double> rpm, float rewardMinutes, IReadOnlyList<StatType> allowed)
        {
            if (allowed == null || allowed.Count == 0) return default;
            var choices = allowed.Where(type => rpm.TryGetValue(type, out var value) && value > 0.0).ToList();
            if (choices.Count == 0) return default;
            float minutes = Math.Max(0f, rewardMinutes);
            var type = choices[UnityEngine.Random.Range(0, choices.Count)];
            double value = rpm[type] * minutes;
            if (value > 0) value = Math.Floor(value);
            return (true, type, minutes, value);
        }
        private static float GetRewardMinutes(ForecastAutoBalanceInput input, float progress) => input.RewardMinutesMax <= 0 && input.RewardMinutesMin <= 0 ? input.RewardMinutes : input.RewardMinutesMin + Mathf.Clamp01(progress) * (input.RewardMinutesMax - input.RewardMinutesMin);
    }
}
