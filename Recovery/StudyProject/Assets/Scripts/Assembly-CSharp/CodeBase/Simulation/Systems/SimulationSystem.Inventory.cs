using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Balance;
using Codebase.Enemy;
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
            if (RebuildParams.AutoBalanceMode == AutoBalanceMode.Regenerate && SimulationHarness.CurrentJob != null
                && SimulationHarness.RebuildResetMode != "reenter_play") error = "Regenerate requires reenter_play";
            if (error != null) { RebuildFail(SimulationHarness.ExitBadRequest, error); return; }
            RebuildInventoryBalanceBefore = JsonUtility.ToJson(combatBalanceConfig);
        }

        internal JObject RebuildFinishInventory(bool probe)
        {
            string after = JsonUtility.ToJson(combatBalanceConfig);
            JObject generator = probe ? RebuildInventoryGeneratorProbe() : null;
            if (RebuildInventoryBalanceBefore != null && RebuildParams.AutoBalanceMode == AutoBalanceMode.Regenerate)
                JsonUtility.FromJsonOverwrite(RebuildInventoryBalanceBefore, combatBalanceConfig);
            return new JObject {
                ["mode"] = (int)RebuildParams.AutoBalanceMode,
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
            if (RebuildParams.SlotRpmOrder == SlotRpmOrder.SlotAfterRpm && RebuildPendingItemCopies.Count > 0) ProcessInventory();
            RebuildInventoryAfterRpm = false;
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
            if (RebuildLastTickFast && RebuildParams.SlotRpmOrder == SlotRpmOrder.SlotAfterRpm && !RebuildInventoryAfterRpm) return;
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
                while ((int)item.Config.Rarity <= (int)Rarity.Legendary && (!item.Evolved || item.Level < combatBalanceConfig.MaxEvolutionLevel))
                {
                    int level = item.Evolved ? item.Level + 1 : 1;
                    float target = RebuildInventoryTarget(item, level);
                    string id = item.Key + ":" + level;
                    if (day >= target && RebuildInventoryTargets.Add(id))
                        RebuildInventoryHeadroom.Add(new JObject { ["key"] = item.Key, ["level"] = level, ["day"] = day,
                            ["target"] = target, ["copies"] = item.Copies, ["shards"] = item.LevelsToEvolve,
                            ["headroom"] = item.Copies - item.LevelsToEvolve });
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
            combatBalanceConfig.TrySetArmorBaseValue(item.Config.Rarity, (float)(0.8 * GetForecastStatValue(
                global::Codebase.Services.Damage.DamageCalculator.WeaponTypeToDefence(item.Config.DamageType, false))));
        }

        [SimulationRebuild(Provenance.Calibrated, "pending:simulation.evolution_balance")]
        private void ApplyAutoBalanceForEvolution(ItemModel item)
        {
            if (RebuildParams.AutoBalanceMode != AutoBalanceMode.Regenerate) return;
            double stat = GetForecastStatValue(item.IsArmor
                ? global::Codebase.Services.Damage.DamageCalculator.WeaponTypeToDefence(item.Config.DamageType, false) : item.Config.DamageStatTypeFlat);
            double[] rarity = { 1.0, 1.25, 1.5, 1.875, 2.5 };
            double type = item.Config.DamageType == WeaponType.Cutting ? 0.8 : item.Config.DamageType == WeaponType.Buckshot ? 0.573 : 1.0;
            double h = item.IsArmor ? 0.8 * stat : type * rarity[(int)item.Config.Rarity] * item.Config.AttackCooldown * stat;
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
                return new JObject { ["rows"] = rows };
            }
            finally { JsonUtility.FromJsonOverwrite(before, combatBalanceConfig); }
        }
    }
}
