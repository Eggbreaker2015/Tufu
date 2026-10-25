using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Simulation.Forecast;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Rewards.Commands
{
    public struct BakeRewardsCommandRunner : ICommandHandler<BakeRewardsCommand>
    {
        [Inject] private SimulationSystem simulationSystem;
        [Inject] private ItemsProvider itemsProvider;
        [Inject] private EventsRepository eventsRepository;

        // Original ELF VA0x141b448, literal enum order1,2,8,6,12,16,14,20.
        private static readonly List<StatType> randomStats = new List<StatType>
        {
            StatType.Health, StatType.HealthRegen,
            StatType.CutDamageFlat, StatType.StabDamageFlat, StatType.BuckshotDamageFlat,
            StatType.CutDefenceFlat, StatType.StabDefenceFlat, StatType.BuckshotDefenceFlat
        };

        public void Execute(BakeRewardsCommand command)
        {
            // Original0x141a824: only a zero multiplier defaults to1; negative values are preserved.
            var rewards = command.BakeRosterEvent ? GetRosterEventRewards() : command.Rewards;
            if (rewards == null) return;
            float multiplier = command.Multiplier == 0f ? 1f : command.Multiplier;
            foreach (var reward in rewards)
            {
                if (reward is StatReward stat) BakeStatReward(stat, multiplier, command.ForceBake);
                else if (reward is ItemReward item) BakeItemReward(item, command.ForceBake);
            }
        }

        private IEnumerable<RewardBase> GetRosterEventRewards()
        {
            // Original0x141ac08 and closure0x141b5c0: flatten real persisted stage rewards only.
            var plan = eventsRepository.EventPlan;
            if (!plan.HasValue || plan.Value.CrystalStages == null) return null;
            var provider = itemsProvider;
            return plan.Value.CrystalStages.SelectMany(stage => stage.Rewards != null
                ? (IEnumerable<RewardBase>)ForecastCrystalRewardEntryHelper.ToRewards(stage.Rewards, provider)
                : Enumerable.Empty<RewardBase>());
        }

        private void BakeStatReward(StatReward statReward, float multiplier, bool forceBake)
        {
            // Original0x141ae0c: authored fixed rewards are unchanged; cached dynamic rewards survive unless forced.
            if ((!statReward.IsRandom && !statReward.UseGpm) || string.IsNullOrEmpty(statReward.SaveKey)
                || (!forceBake && statReward.BakedData != null)) return;
            if (BakedRewardStorage.Has(statReward.SaveKey) && !forceBake)
            {
                statReward.BakedData = BakedRewardStorage.Load(statReward.SaveKey);
                return;
            }
            var baked = CreateBakedStatReward(statReward, multiplier);
            if (baked == null)
            {
                BakedRewardStorage.Remove(statReward.SaveKey);
                statReward.BakedData = null;
                return;
            }
            BakedRewardStorage.Save(statReward.SaveKey, baked);
            statReward.BakedData = baked;
        }

        private BakedStatReward CreateBakedStatReward(StatReward reward, float multiplier)
        {
            // Original0x141b170: select from allowed/default stats, filtering by real positive RPM for GPM rewards.
            var type = reward.Type;
            if (reward.IsRandom)
            {
                var choices = reward.AllowedStats != null && reward.AllowedStats.Count > 0 ? reward.AllowedStats : randomStats;
                if (reward.UseGpm)
                {
                    var rpmValues = simulationSystem.RPMCalculatedValue;
                    choices = choices.Where(stat => rpmValues.TryGetValue(stat, out var rpm) && rpm > 0.0).ToList();
                }
                if (choices.Count == 0) return null;
                type = choices[UnityEngine.Random.Range(0, choices.Count)];
            }
            float value;
            if (reward.UseGpm)
            {
                if (!simulationSystem.RPMCalculatedValue.TryGetValue(type, out var rpm) || rpm <= 0.0) return null;
                // Native double arithmetic and PrettyRound(2,1,4), then minimum1.
                value = Math.Max(1, (rpm * (double)reward.Value * (double)multiplier).PrettyRound(2, 1, 4));
            }
            else value = reward.Value * multiplier;
            return new BakedStatReward { Type = type, Value = value };
        }

        private void BakeItemReward(ItemReward itemReward, bool forceBake)
        {
            // Original0x141af2c and predicate0x141b6f0: exact slot/rarity, no invented fallback item.
            if (!itemReward.IsRandom || string.IsNullOrEmpty(itemReward.SaveKey)) return;
            if (BakedRewardStorage.HasItem(itemReward.SaveKey) && !forceBake)
            {
                itemReward.BakedData = BakedRewardStorage.LoadItem(itemReward.SaveKey);
                return;
            }
            var choices = itemsProvider.Items.Where(item => item.SlotType == itemReward.SlotType && item.Rarity == itemReward.Rarity).ToList();
            if (choices.Count == 0) return;
            var baked = new BakedItemReward { ItemKey = choices[UnityEngine.Random.Range(0, choices.Count)].Key };
            BakedRewardStorage.SaveItem(itemReward.SaveKey, baked);
            itemReward.BakedData = baked;
        }
    }
}
