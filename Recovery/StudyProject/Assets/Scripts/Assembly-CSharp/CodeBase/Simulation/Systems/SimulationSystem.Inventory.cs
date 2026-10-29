using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Balance;
using Codebase.Enemy;
using Codebase.Unit;
using Codebase.Player.Inventory;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
    public partial class SimulationSystem
    {
        private string RebuildRollStream;
        private string RebuildInventoryBalanceBefore;
        private bool RebuildInventoryAfterRpm;
        private bool RebuildInventoryDeferred;
        private int RebuildInventoryRewardDay = -1;
        private readonly HashSet<int> RebuildInventoryPendingSlots = new HashSet<int>();
        private readonly JArray RebuildInventoryRewards = new JArray();
        private readonly JArray RebuildInventoryHeadroom = new JArray();
        private readonly HashSet<string> RebuildInventoryTargets = new HashSet<string>();
        private readonly Dictionary<string, double> RebuildInventoryExpectations = new Dictionary<string, double>();
        private readonly Dictionary<string, long> RebuildInventoryCopies = new Dictionary<string, long>();

        private void RebuildPrepareInventory()
        {
            string error = RebuildParams.RebuildValidateInventory();
            if (RebuildParams.AutoBalanceMode == AutoBalanceMode.Regenerate
                && (SimulationHarness.CurrentJob == null || SimulationHarness.RebuildResetMode != "reenter_play")) error = "Regenerate requires reenter_play";
            if (error != null) { RebuildFail(SimulationHarness.ExitBadRequest, error); return; }
            RebuildInventoryBalanceBefore = JsonUtility.ToJson(combatBalanceConfig);
            combatBalanceConfig.RebuildClearCapture();
        }

        internal JObject RebuildFinishInventory(bool probe)
        {
            string after = null;
            JObject generator = null;
            try
            {
                if (RebuildInventoryBalanceBefore != null && RebuildParams.AutoBalanceMode == AutoBalanceMode.Regenerate)
                    combatBalanceConfig.FillRemainingEvolutionsByLastValue();
                after = JsonUtility.ToJson(combatBalanceConfig);
                generator = probe ? RebuildInventoryGeneratorProbe() : null;
            }
            finally
            {
                if (RebuildInventoryBalanceBefore != null && RebuildParams.AutoBalanceMode == AutoBalanceMode.Regenerate)
                    JsonUtility.FromJsonOverwrite(RebuildInventoryBalanceBefore, combatBalanceConfig);
                combatBalanceConfig.RebuildClearCapture();
            }
            return new JObject {
                ["schema"] = "tufu.sim.inventory/1",
                ["mode"] = (int)RebuildParams.AutoBalanceMode,
                ["balance_before_sha256"] = RebuildInventoryBalanceBefore == null ? null : SimulationTrace.Sha256(RebuildInventoryBalanceBefore),
                ["balance_during_sha256"] = after == null ? null : SimulationTrace.Sha256(after),
                ["balance_restored_sha256"] = SimulationTrace.Sha256(JsonUtility.ToJson(combatBalanceConfig)),
                ["frozen_unchanged"] = RebuildInventoryBalanceBefore == null || after == RebuildInventoryBalanceBefore,
                ["restored"] = RebuildInventoryBalanceBefore == null || JsonUtility.ToJson(combatBalanceConfig) == RebuildInventoryBalanceBefore,
                ["rewards"] = RebuildInventoryRewards,
                ["headroom"] = RebuildInventoryHeadroom,
                ["totals"] = new JArray(RebuildInventoryExpectations.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p =>
                    (object)new JObject { ["key"] = p.Key, ["expectation"] = p.Value, ["copies"] = RebuildInventoryCopies[p.Key] }).ToArray()),
                ["generator"] = generator,
            };
        }

        [SimulationRebuild(Provenance.Inferred, "pending:simulation.fractional_count")]
        public int RollFractionalCount(double value)
        {
            if (string.IsNullOrEmpty(RebuildRollStream)) throw new InvalidOperationException("item roll requires a Key stream");
            int count = RebuildRandom.Roll(RebuildRollStream, value);
            RebuildInventoryExpectations.TryGetValue(RebuildRollStream, out double expected);
            RebuildInventoryCopies.TryGetValue(RebuildRollStream, out long copies);
            RebuildInventoryExpectations[RebuildRollStream] = expected + value;
            RebuildInventoryCopies[RebuildRollStream] = copies + count;
            RebuildInventoryRewards.Add(new JObject { ["day"] = timeChunkIndex, ["key"] = RebuildRollStream, ["expectation"] = value, ["copies"] = count,
                ["phase"] = RebuildLastTickFast ? "fast" : "slow" });
            return count;
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.item_chain")]
        private static double GetChainedItemBaseCount(int baseItemsCount, float value)
        {
            if (baseItemsCount <= 0) return 0.0;
            if (value == 1f) return baseItemsCount;
            return (1.0 - Math.Pow(value, baseItemsCount)) / (1.0 - value);
        }

        private double RebuildItemBaseCount(int count, float chain)
        {
            return RebuildParams.EmissionModel == EmissionModel.E1 ? count : GetChainedItemBaseCount(count, chain);
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.weapon_slots")]
        private void UnlockSimulationWeaponSlot(int locationNumber)
        {
            if (RebuildLastTickFast && RebuildParams.SlotRpmOrder == SlotRpmOrder.SlotAfterRpm && !RebuildInventoryAfterRpm)
            { RebuildInventoryPendingSlots.Add(locationNumber); return; }
            for (int i = 0; i < 2; i++)
            {
                if (locationNumber < RebuildParams.WeaponSlotUnlockLocations[i]) continue;
                ItemSlotModel slot = playerModel.ItemSlots.FirstOrDefault(s => s.Key == "weapon_" + (i + 3));
                if (slot == null || slot.IsUnlocked) continue;
                slot.IsUnlocked = true;
                RebuildTraceSlotUnlock(slot);
                RebuildCount("slot_unlocks");
            }
        }

        private void RebuildInventoryAfterDayRpm()
        {
            RebuildInventoryAfterRpm = true;
            foreach (int location in RebuildInventoryPendingSlots.OrderBy(x => x)) UnlockSimulationWeaponSlot(location);
            RebuildInventoryPendingSlots.Clear();
            if (RebuildInventoryDeferred) ProcessInventory();
            RebuildInventoryAfterRpm = false;
            RebuildInventoryDeferred = false;
            RebuildInventoryRewardDay = -1;
        }

        private void RebuildFlushInventoryItems()
        {
            RebuildInventoryRewardDay = timeChunkIndex;
            foreach (var pair in fastTickChunkItemRewards.OrderBy(p => p.Key.Key, StringComparer.Ordinal))
            {
                RebuildRollStream = pair.Key.Key;
                int copies = RollFractionalCount(pair.Value);
                RebuildPendingItemCopies.TryGetValue(pair.Key, out int pending);
                RebuildPendingItemCopies[pair.Key] = checked(pending + copies);
            }
            // Timed evolutions must also be checked on days without positive item rewards.
            ProcessInventory();
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.process_inventory")]
        private void ProcessInventory()
        {
            if (RebuildLastTickFast && RebuildParams.SlotRpmOrder == SlotRpmOrder.SlotAfterRpm && !RebuildInventoryAfterRpm)
            { RebuildInventoryDeferred = true; return; }
            foreach (var pair in RebuildPendingItemCopies.OrderBy(p => p.Key.Key, StringComparer.Ordinal))
            {
                if (pair.Value <= 0) continue;
                playerModel.AddItem(new ItemModel(playerModel.Lifetime, pair.Key) { Copies = pair.Value }, false, true);
            }
            RebuildPendingItemCopies.Clear();
            float day = RebuildInventoryRewardDay >= 0 ? RebuildInventoryRewardDay : GetBalanceTimingMinutes();
            if (RebuildParams.AutoBalanceMode == AutoBalanceMode.Regenerate)
                combatBalanceConfig.UpdateEvolutionShardsRequirements(day, playerModel.Items);
            foreach (ItemModel item in playerModel.Items)
            {
                TryApplyArmorBaseAutoBalance(item);
                if ((int)item.Config.Rarity <= (int)Rarity.Legendary)
                    for (int targetLevel = 1; targetLevel <= combatBalanceConfig.MaxEvolutionLevel; targetLevel++)
                    {
                        float targetDay = RebuildInventoryTarget(item, targetLevel);
                        string targetId = item.Key + ":" + targetLevel;
                        if (day < targetDay || !RebuildInventoryTargets.Add(targetId)) continue;
                        int shards = combatBalanceConfig.GetEvolutionShardsRequirement(item.IsArmor, item.Config.Rarity, targetLevel);
                        RebuildInventoryHeadroom.Add(new JObject { ["key"] = item.Key, ["level"] = targetLevel, ["day"] = day,
                            ["target"] = targetDay, ["current_level"] = item.Evolved ? item.Level : 0,
                            ["copies"] = item.Copies, ["shards"] = shards, ["headroom"] = item.Copies - shards });
                    }
                while ((int)item.Config.Rarity <= (int)Rarity.Legendary && (!item.Evolved || item.Level < combatBalanceConfig.MaxEvolutionLevel))
                {
                    int level = item.Evolved ? item.Level + 1 : 1;
                    float target = RebuildInventoryTarget(item, level);
                    bool timed = RebuildParams.EvolutionTrigger != EvolutionTrigger.Greedy;
                    float lag = RebuildParams.EvolutionTrigger == EvolutionTrigger.TimedLag ? 1f : 0f;
                    if (timed && day < target + lag) break;
                    if (item.Copies < item.LevelsToEvolve && RebuildParams.EvolutionTrigger != EvolutionTrigger.TimedForce) break;
                    ApplyAutoBalanceForEvolution(item);
                    int required = item.LevelsToEvolve;
                    item.Copies = Math.Max(0, item.Copies - required);
                    item.Level = level;
                    item.Evolved = true;
                    RebuildTraceEvolve(item);
                }
            }
        }

        private float RebuildInventoryTarget(ItemModel item, int level)
        {
            string name = item.Config.Rarity + (item.IsArmor ? "Armor" : "") + "EvolutionTargetTimings";
            float[] timings = (float[])typeof(CombatBalanceConfig).GetField(name).GetValue(combatBalanceConfig);
            return timings[level - 1];
        }

        [SimulationRebuild(Provenance.Inferred, "pending:simulation.armor_base_balance")]
        private void TryApplyArmorBaseAutoBalance(ItemModel item)
        {
            if (RebuildParams.AutoBalanceMode != AutoBalanceMode.Regenerate || !item.IsArmor) return;
            combatBalanceConfig.TrySetArmorBaseValue(item.Config.Rarity, (float)(RebuildParams.ArmorHeuristicMultiplier * GetForecastStatValue(
                global::Codebase.Services.Damage.DamageCalculator.WeaponTypeToDefence(item.Config.DamageType, false))));
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.evolution_balance")]
        private void ApplyAutoBalanceForEvolution(ItemModel item)
        {
            if (RebuildParams.AutoBalanceMode != AutoBalanceMode.Regenerate) return;
            double stat = GetForecastStatValue(item.IsArmor
                ? global::Codebase.Services.Damage.DamageCalculator.WeaponTypeToDefence(item.Config.DamageType, false) : item.Config.DamageStatTypeFlat);
            int typeIndex = item.Config.DamageType == WeaponType.Cutting ? 1 : item.Config.DamageType == WeaponType.Buckshot ? 2 : 0;
            double type = RebuildParams.WeaponHeuristicTypeMultipliers[typeIndex];
            double h = item.IsArmor ? RebuildParams.ArmorHeuristicMultiplier * stat
                : type * RebuildParams.WeaponHeuristicRarityMultipliers[(int)item.Config.Rarity] * item.Config.AttackCooldown * stat;
            combatBalanceConfig.ApplyEvolutionAutoBalance(item.IsArmor, item.Config.DamageType, item.Config.Rarity, item.Evolved ? item.Level + 1 : 1, h, stat, timeChunkIndex);
        }

        [SimulationRebuild(Provenance.Inferred, "pending:simulation.slow_item_reward")]
        private int GetSpawnerItemRewardCopies(EnemySpawner spawner)
        {
            RebuildRollStream = spawner.Config.ItemReward.Key;
            return RollFractionalCount(GetItemEmissionCoefficient(GetBalanceTimingMinutes(), spawner.Config.ItemReward));
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.item_emission")]
        private float GetItemEmissionCoefficient(float timing, ItemConfig item)
        {
            BalanceInterpolationType type = item.Rarity == Rarity.Legendary ? BalanceInterpolationType.ItemLegendaryEmission : BalanceInterpolationType.ItemEmission;
            return 1f + GetSimulationInterpolationCoefficient(timing, type)
                + (IsPreferredItemEmissionType(item) ? GetSimulationInterpolationCoefficient(timing, BalanceInterpolationType.ItemEmissionByType) : 0f);
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.item_preferred")]
        private bool IsPreferredItemEmissionType(ItemConfig item)
        {
            var location = GetCachedLocation(playerModel.LastUnlockedLocation.Number);
            bool armor = item.SlotType == ItemSlotType.Helmet || item.SlotType == ItemSlotType.Shield;
            bool useArmor = RebuildParams.PreferredMapping == PreferredMapping.Swapped ? !armor : armor;
            return item.DamageType == (useArmor ? location.PreferredArmorType : location.PreferredWeaponType);
        }

        private JObject RebuildInventoryGeneratorProbe()
        {
            string before = JsonUtility.ToJson(combatBalanceConfig);
            var rows = new JArray();
            try
            {
                foreach (bool armor in new[] { false, true })
                {
                    var source = (armor ? combatBalanceConfig.EvolutionArmorValues : combatBalanceConfig.EvolutionDamageValues).ToList();
                    foreach (var row in source.Where(x => (int)x.Rarity <= 4 && x.DamageType != WeaponType.Universal).OrderBy(x => x.EvolutionLevel))
                    {
                        var shards = armor ? combatBalanceConfig.EvolutionArmorShardsRequirements : combatBalanceConfig.EvolutionDamageShardsRequirements;
                        int idx = shards.FindIndex(x => x.Rarity == row.Rarity && x.EvolutionLevel == row.EvolutionLevel);
                        shards[idx] = new EvolutionShardsRequirement { Rarity = row.Rarity, EvolutionLevel = row.EvolutionLevel, Value = row.ReferenceShardsRequirement };
                        combatBalanceConfig.ApplyEvolutionAutoBalance(armor, row.DamageType, row.Rarity, row.EvolutionLevel,
                            row.ReferenceHeuristicValue, row.ReferenceStatValue, row.ReferenceTimeChunkIndex);
                        var generated = (armor ? combatBalanceConfig.EvolutionArmorValues : combatBalanceConfig.EvolutionDamageValues).First(x => x.Rarity == row.Rarity && x.DamageType == row.DamageType && x.EvolutionLevel == row.EvolutionLevel);
                        rows.Add(new JObject { ["armor"] = armor, ["type"] = (int)row.DamageType, ["rarity"] = (int)row.Rarity,
                            ["level"] = row.EvolutionLevel, ["value"] = generated.Value, ["copy_value"] = generated.CopyValue });
                    }
                }
                var examples = new JArray();
                foreach (Rarity rarity in new[] { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary })
                {
                    EnemySpawner source = sortedSpawners.First(x => x.Config.ItemReward != null && x.Config.ItemReward.Rarity == rarity && !x.Config.IsVip && !x.Config.IsEvent);
                    int n = GetSpawnerChargesLimit(source);
                    for (int day = 4; day < 11; day++)
                    {
                        float probability = GetSimulationInterpolationCoefficient(day, BalanceInterpolationType.ItemChainProbability);
                        float emission = GetItemEmissionCoefficient(day, source.Config.ItemReward);
                        examples.Add(new JObject { ["day"] = day, ["rarity"] = (int)rarity, ["charges"] = n,
                            ["chain"] = GetChainedItemBaseCount(n, probability) * emission, ["e1"] = n * (double)emission });
                    }
                }
                var islandDays = new JArray();
                for (float island = 1f; island <= combatBalanceConfig.IslandTargetDays.Length + 1; island += 0.5f)
                    islandDays.Add(new JArray(island, combatBalanceConfig.ConvertIslandTimingToDays(island)));
                Stat stat = playerModel.Unit.Stats.First(x => x.Type == StatType.Health);
                double snapshotBefore = RebuildSnapshotStats().First(x => x.Key == stat.Type).Value;
                double totalBefore = stat.FastValue;
                var equipment = new StatModifier { Name = "__inventory_x6_probe", StatType = stat.Type, Source = ModifierSource.Item, ModifierType = ModifierType.Flat, Value = 37f };
                stat.AddModifier(equipment);
                JObject x6;
                try { x6 = new JObject { ["before"] = snapshotBefore, ["with_equipment"] = RebuildSnapshotStats().First(x => x.Key == stat.Type).Value,
                    ["total_before"] = totalBefore, ["total_after"] = stat.FastValue }; }
                finally { stat.RemoveModifier(equipment); }
                return new JObject { ["rows"] = rows, ["chain_examples"] = examples, ["island_days"] = islandDays, ["x6"] = x6 };
            }
            finally { JsonUtility.FromJsonOverwrite(before, combatBalanceConfig); }
        }
    }
}
