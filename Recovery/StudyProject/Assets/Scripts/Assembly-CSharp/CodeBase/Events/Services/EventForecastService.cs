using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Enemy;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Codebase.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Codebase.Events.Services
{
    public class EventForecastService
    {
        private ForecastEventPlan? eventPlan;
        private EventsUd Ud => UserDataProvider.Resolve<EventsUd>();
        public string SnapshotDataValue
        {
            get => Ud.SnapshotData.Value;
            set
            {
                Ud.SnapshotData.Value = value;
                eventPlan = string.IsNullOrEmpty(value) ? (ForecastEventPlan?)null : JsonUtility.FromJson<ForecastEventPlan>(value);
            }
        }
        public bool HasEventPlan => !string.IsNullOrEmpty(SnapshotDataValue);
        public ForecastEventPlan? EventPlan
        {
            get
            {
                if (eventPlan.HasValue) return eventPlan;
                if (string.IsNullOrEmpty(SnapshotDataValue)) return null;
                // Original 0x14e33d0 catches malformed persisted JSON and returns no plan.
                try { eventPlan = JsonUtility.FromJson<ForecastEventPlan>(SnapshotDataValue); }
                catch (ArgumentException) { return null; }
                return eventPlan;
            }
        }
        public bool TryApplyCrystalStages(EventModel model)
        {
            // Original 0x14e3648 validates the persisted plan; it does not rewrite authored ScriptableObjects.
            return model?.Def?.CrystalRoster != null && model.Def.CrystalRoster.Stages != null && EventPlan.HasValue
                && EventPlan.Value.CrystalStages != null && EventPlan.Value.CrystalStages.Count > 0
                && EventPlan.Value.CrystalMilestones != null && EventPlan.Value.CrystalMilestones.Count > 0;
        }
        public ForecastEventEnemyInfo? GetEventEnemyInfo(EventUnitType unitType, EventModel currentEvent)
        {
            var plan = EventPlan;
            if (!plan.HasValue) return null;
            switch (unitType)
            {
                case EventUnitType.Mob: return plan.Value.Golem;
                case EventUnitType.MiniBoss: return GetMiniBossInfo(plan.Value, currentEvent);
                case EventUnitType.Boss: return plan.Value.GrandBoss;
                default: return null;
            }
        }
        public ForecastEventRewardInfo? GetEventRewardInfo(EventUnitType unitType, EventModel currentEvent)
        {
            var plan = EventPlan;
            if (!plan.HasValue || unitType != EventUnitType.MiniBoss || currentEvent == null || plan.Value.Bosses == null || plan.Value.Bosses.Count == 0) return null;
            return plan.Value.Bosses[Math.Clamp(currentEvent.Progress, 0, plan.Value.Bosses.Count - 1)].Reward;
        }
        private ForecastEventEnemyInfo? GetMiniBossInfo(ForecastEventPlan plan, EventModel currentEvent)
        {
            if (currentEvent == null || plan.Bosses == null || plan.Bosses.Count == 0) return null;
            return plan.Bosses[Math.Clamp(currentEvent.Progress, 0, plan.Bosses.Count - 1)].Enemy;
        }
        public bool ShouldRecalculateRewards(EventModel model, EventDifficulty difficulty)
        {
            var config = model?.Def?.GetDifficultySettings(difficulty);
            if (config?.Rewards == null) return false;
            foreach (var reward in config.Rewards.OfType<StatReward>())
                if ((reward.IsRandom || reward.UseGpm) && !string.IsNullOrEmpty(reward.SaveKey)
                    && (!BakedRewardStorage.Has(reward.SaveKey) || BakedRewardStorage.Load(reward.SaveKey) == null || BakedRewardStorage.Load(reward.SaveKey).Value <= 0f))
                    return true;
            return false;
        }
        public async UniTask BuildForecastAndBakeRewardsIfNeededAsync(EventModel model, EventDifficulty difficulty)
        {
            // Original 0x14e7d54: bake only missing dynamic completion rewards, preserve valid baked rewards.
            var config = model?.Def?.GetDifficultySettings(difficulty);
            if (config == null) return;
            ApplyBakedRewards(config.Rewards);
            if (!ShouldRecalculateRewards(model, difficulty)) return;
            var system = SimulationSystem.Instance;
            if (system == null) return;
            var input = config.AutoBalanceInput;
            input.ForecastLocationNumber = system.playerModel.BossProgressLocation.Number;
            input.BossSnapshotTimingStep = 0f;
            SetCombatTypes(ref input, model);
            var result = await ForecastApi.BuildAsync(system, config.MinutesAhead, config.StepMinutes, input, config.EnemyPresets, config.RewardAllowedStats, config.SnapshotsPerFrame);
            var last = result.Snapshots[result.Snapshots.Count - 1];
            foreach (var reward in config.Rewards.OfType<StatReward>())
            {
                if ((!reward.IsRandom && !reward.UseGpm) || string.IsNullOrEmpty(reward.SaveKey)) continue;
                if (BakedRewardStorage.Has(reward.SaveKey))
                {
                    var old = BakedRewardStorage.Load(reward.SaveKey);
                    if (old != null && old.Value > 0f) { reward.BakedData = old; continue; }
                    BakedRewardStorage.Remove(reward.SaveKey);
                    reward.BakedData = null;
                }
                var type = reward.Type;
                if (reward.IsRandom && config.RewardAllowedStats != null && config.RewardAllowedStats.Count > 0)
                {
                    var choices = config.RewardAllowedStats.Where(value => !reward.UseGpm || GetGpm(last, value) > 0).ToList();
                    if (choices.Count > 0) type = choices[UnityEngine.Random.Range(0, choices.Count)];
                }
                double value = reward.Value * (reward.UseGpm ? GetGpm(last, type) : 1.0);
                if (value <= 0) { reward.BakedData = null; continue; }
                reward.BakedData = new BakedStatReward { Type = type, Value = (float)value };
                BakedRewardStorage.Save(reward.SaveKey, reward.BakedData);
            }
        }
        public async UniTask<string> BuildEventPlanAsync(EventModel model)
        {
            // Original 0x14e6d34. The repository owns persistence of the returned JSON.
            var system = SimulationSystem.Instance;
            if (system == null) return null;
            var roster = model?.Def?.CrystalRoster;
            bool crystal = EventKeys.IsCrystal(model?.Key) && roster != null;
            ForecastAutoBalanceInput input;
            ForecastEnemyPresets presets;
            IReadOnlyList<StatType> allowed;
            int minutesAhead, stepMinutes, perFrame;
            if (crystal)
            {
                var settings = roster.ForecastSettings;
                input = settings.AutoBalanceInput;
                input.ForecastLocationNumber = roster.Leagues[model.RosterLeagueIndex].ForecastLocationNumber;
                input.BossSnapshotTimingStep = 0.25f;
                presets = settings.EnemyPresets;
                allowed = settings.RewardAllowedStats;
                minutesAhead = settings.MinutesAhead; stepMinutes = settings.StepMinutes; perFrame = settings.SnapshotsPerFrame;
            }
            else
            {
                var settings = model?.DifficultyDef;
                if (settings == null) return null;
                input = settings.AutoBalanceInput;
                input.ForecastLocationNumber = system.playerModel.BossProgressLocation.Number;
                input.BossSnapshotTimingStep = 0f;
                presets = settings.EnemyPresets;
                allowed = settings.RewardAllowedStats;
                minutesAhead = settings.MinutesAhead; stepMinutes = settings.StepMinutes; perFrame = settings.SnapshotsPerFrame;
            }
            SetCombatTypes(ref input, model);
            if (crystal)
            {
                input.EventBossMinutes = roster.BossMinutes;
                if (roster.BossMinutes != null && roster.BossMinutes.Count > 0)
                {
                    input.EventLastBossMinute = roster.BossMinutes[roster.BossMinutes.Count - 1];
                    input.EventEndMinute = Math.Max(input.EventEndMinute, input.EventLastBossMinute);
                }
            }
            var result = await ForecastApi.BuildAsync(system, minutesAhead, stepMinutes, input, presets, allowed, perFrame);
            var plan = result.EventPlan;
            if (crystal)
            {
                plan.LeagueIndex = model.RosterLeagueIndex;
                int nearest = Enumerable.Range(0, result.Snapshots.Count)
                    .OrderBy(index => Math.Abs(input.ForecastLocationNumber + input.BossSnapshotTimingStep * index - system.playerModel.BossProgressLocation.Number)).First();
                var golem = result.Snapshots[nearest].Enemies.First(enemy => enemy.Kind == ForecastEnemyKind.EnemyGolem);
                plan.Golem.Health = golem.Health;
                plan.Golem.Damage = golem.Damage;
                if (plan.Bosses != null && plan.Bosses.Count > 0)
                {
                    var items = GameContext.Get<ItemsProvider>();
                    plan.CrystalStages = BuildCrystalStages(roster, result.Snapshots, plan.Bosses, roster.Leagues[model.RosterLeagueIndex].ForecastLocationNumber, items);
                    plan.CrystalMilestones = BuildCrystalMilestones(roster, result.Snapshots[result.Snapshots.Count - 1], items);
                }
            }
            return JsonUtility.ToJson(plan);
        }
        private static void SetCombatTypes(ref ForecastAutoBalanceInput input, EventModel model)
        {
            input.EnemyAttackType = model.Data.WeaponType.Value;
            var resist = model.Data.ResistType.Value;
            input.ResistStabbing = resist == WeaponType.Stabbing ? -0.9f : 0f;
            input.ResistCutting = resist == WeaponType.Cutting ? -0.9f : 0f;
            input.ResistBuckshot = resist == WeaponType.Buckshot ? -0.9f : 0f;
        }
        private List<ForecastCrystalStagePlan> BuildCrystalStages(CrystalEventRosterConfig cfg, List<ForecastSnapshotExtended> snapshots, List<ForecastEventBossInfo> bosses, int rewardsLocationNumber, ItemsProvider itemsProvider)
        {
            var generated = new CrystalEventRosterGenerator().Generate(GameContext.Get<Codebase.Player.Models.PlayerModel>(), rewardsLocationNumber);
            var result = new List<ForecastCrystalStagePlan>(bosses.Count);
            for (int index = 0; index < bosses.Count; index++)
            {
                float minute = bosses[index].Enemy.Minute;
                var rewards = BuildCrystalRewards(cfg, index, FindNearestSnapshot(snapshots, minute), itemsProvider);
                // Original removes non-mythical authored random item slots before appending generated inventory rewards.
                rewards.RemoveAll(entry => entry.Kind >= ForecastCrystalRewardKind.WeaponTier && entry.Kind <= ForecastCrystalRewardKind.ArmorTier && entry.Tier != 7);
                result.Add(new ForecastCrystalStagePlan { Minute = minute, Rewards = generated[index].Concat(rewards).ToList() });
            }
            return result;
        }
        private List<ForecastCrystalMilestonePlan> BuildCrystalMilestones(CrystalEventRosterConfig cfg, ForecastSnapshotExtended snap, ItemsProvider itemsProvider) => cfg.Milestones.Select(milestone => new ForecastCrystalMilestonePlan { RequiredCrystals = milestone.RequiredCrystals, Rewards = BuildCrystalRewards(milestone.Rewards, snap, itemsProvider) }).ToList();
        private static ForecastSnapshotExtended FindNearestSnapshot(List<ForecastSnapshotExtended> snapshots, float minute) => snapshots == null || snapshots.Count == 0 ? default : snapshots.OrderBy(snapshot => Math.Abs(snapshot.Snapshot.Playtime - minute)).First();
        private List<ForecastCrystalRewardEntry> BuildCrystalRewards(CrystalEventRosterConfig cfg, int stageIndex, ForecastSnapshotExtended snap, ItemsProvider itemsProvider)
        {
            if (stageIndex < 0 || cfg.Stages == null || stageIndex >= cfg.Stages.Count || cfg.Stages[stageIndex].Rewards == null) return new List<ForecastCrystalRewardEntry>(6);
            return BuildCrystalRewards(cfg.Stages[stageIndex].Rewards, snap, itemsProvider);
        }
        private List<ForecastCrystalRewardEntry> BuildCrystalRewards(IReadOnlyList<RewardBase> rewards, ForecastSnapshotExtended snap, ItemsProvider itemsProvider)
        {
            var result = new List<ForecastCrystalRewardEntry>(6);
            foreach (var reward in rewards)
            {
                if (reward is StatReward stat) ProcessStatReward(stat, snap, result);
                else if (reward is ChestReward chest && chest.Amount > 0) result.Add(new ForecastCrystalRewardEntry { Kind = ForecastCrystalRewardKind.Chest, Key = chest.Key, Amount = chest.Amount });
                else if (reward is ItemReward item) ProcessItemReward(item, itemsProvider, result);
                else if (reward is EventReward duration && duration.ExtraDurationMinutes > 0) result.Add(new ForecastCrystalRewardEntry { Kind = ForecastCrystalRewardKind.CustomKey, Key = "extra_duration", Amount = duration.ExtraDurationMinutes });
            }
            return result;
        }
        private void ProcessStatReward(StatReward statReward, ForecastSnapshotExtended snap, List<ForecastCrystalRewardEntry> results)
        {
            var resolved = statReward.GetResolvedType();
            if (!resolved.HasValue) return;
            var type = resolved.Value;
            if (statReward.IsRandom && statReward.AllowedStats != null && statReward.AllowedStats.Count > 0)
            {
                var choices = statReward.AllowedStats.Where(value => !statReward.UseGpm || GetGpm(snap, value) > 0).ToList();
                type = choices.Count > 0 ? choices[UnityEngine.Random.Range(0, choices.Count)] : StatType.Health;
            }
            if (statReward.Value <= 0f) return;
            double value = statReward.Value;
            if (statReward.UseGpm)
            {
                value = Math.Max(0.0, GetGpm(snap, type) * statReward.Value).PrettyRound(2, 1, 4);
                statReward.BakedData = new BakedStatReward { Type = type, Value = (float)value };
                BakedRewardStorage.Save(statReward.SaveKey, statReward.BakedData);
            }
            results.Add(new ForecastCrystalRewardEntry
            {
                Kind = ForecastCrystalRewardKind.Stat, CustomModifier = statReward.CustomModifier, Name = statReward.Name,
                StatType = type, Source = statReward.Source, ModifierType = statReward.ModifierType,
                Minutes = statReward.Value, Value = value
            });
        }
        private void ProcessItemReward(ItemReward itemReward, ItemsProvider itemsProvider, List<ForecastCrystalRewardEntry> results)
        {
            if (itemReward.Level < 1) return;
            ItemConfig definition;
            Rarity rarity;
            ItemSlotType slot;
            if (itemReward.IsRandom)
            {
                var choices = itemsProvider.Items.Where(item => item != null && item.SlotType == itemReward.SlotType && item.Rarity == itemReward.Rarity).ToList();
                if (choices.Count == 0) return;
                definition = choices[UnityEngine.Random.Range(0, choices.Count)];
                rarity = itemReward.Rarity; slot = itemReward.SlotType;
            }
            else
            {
                definition = itemReward.Def;
                if (definition == null) return;
                rarity = definition.Rarity; slot = definition.SlotType;
            }
            results.Add(new ForecastCrystalRewardEntry
            {
                Kind = slot == ItemSlotType.Weapon ? ForecastCrystalRewardKind.WeaponTier : slot == ItemSlotType.Shield ? ForecastCrystalRewardKind.ShieldTier : slot == ItemSlotType.Helmet ? ForecastCrystalRewardKind.ArmorTier : ForecastCrystalRewardKind.CustomKey,
                Tier = (int)rarity + 1, Key = definition.Key, Amount = itemReward.Level
            });
        }
        public void ApplyBakedRewards(IReadOnlyList<RewardBase> rewards)
        {
            if (rewards == null) return;
            foreach (var reward in rewards)
            {
                if (reward is StatReward stat)
                {
                    var baked = !string.IsNullOrEmpty(stat.SaveKey) && BakedRewardStorage.Has(stat.SaveKey) ? BakedRewardStorage.Load(stat.SaveKey) : null;
                    stat.BakedData = baked != null && baked.Value > 0 ? baked : null;
                }
                else if (reward is ItemReward item)
                {
                    var baked = !string.IsNullOrEmpty(item.SaveKey) && BakedRewardStorage.HasItem(item.SaveKey) ? BakedRewardStorage.LoadItem(item.SaveKey) : null;
                    item.BakedData = baked != null && !string.IsNullOrEmpty(baked.ItemKey) ? baked : null;
                }
            }
        }
        public void ClearSnapshotData() => SnapshotDataValue = null;
        private static double GetGpm(ForecastSnapshotExtended snap, StatType statType) => snap.Snapshot.Stats == null ? 0.0 : snap.Snapshot.Stats.FirstOrDefault(stat => stat.Key == statType).CurrentGpm;
    }
}
