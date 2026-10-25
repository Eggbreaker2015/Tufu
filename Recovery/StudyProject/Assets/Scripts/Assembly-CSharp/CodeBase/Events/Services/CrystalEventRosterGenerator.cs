using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Simulation.Forecast;
using UnityEngine;

namespace Codebase.Events.Services
{
    public class CrystalEventRosterGenerator
    {
        private const int MinGeneratedBudgetPoints = 600;
        // Original 0x14e1534, private field-RVA blobs SHA256 1CFE6611... and 3DFCFCD5....
        private static readonly int[] RarityPoints = { 2, 3, 5, 10, 25 };
        private static readonly int[] MinRewardNumbersByRarity = { 1, 2, 3, 5, 8 };
        private static readonly List<double>[] RewardSlots =
        {
            new List<double>{2}, new List<double>{3}, new List<double>{5}, new List<double>{5,5},
            new List<double>{6}, new List<double>{8}, new List<double>{10}, new List<double>{10,10},
            new List<double>{8,5}, new List<double>{10,6}, new List<double>{11,7}, new List<double>{12,12,12},
            new List<double>{10,8}, new List<double>{12,10}, new List<double>{14,12}, new List<double>{20,20,20},
            new List<double>{16,14}, new List<double>{18,16}, new List<double>{22,20}, new List<double>{30,30,30}
        };

        // Original 0x14dfca8: scene item-drop budget, existing inventory variants, weighted budget allocation.
        public List<List<ForecastCrystalRewardEntry>> Generate(PlayerModel player, int locationNumber)
        {
            double budget = BuildIslandPoints().Where(pair => pair.Key <= locationNumber).Sum(pair => pair.Value);
            if (budget < MinGeneratedBudgetPoints) budget = MinGeneratedBudgetPoints / Math.Sqrt(MinGeneratedBudgetPoints / budget);
            var variants = BuildRewardVariants(player.Items);
            var assigned = new double[variants.Count];
            var parts = GetRewardParts();
            double totalWeight = parts.Sum(part => part.Weight);
            var result = RewardSlots.Select(slot => new List<ForecastCrystalRewardEntry>()).ToList();
            foreach (var part in parts)
            {
                var chosen = ChooseCandidate(BuildCandidates(part.RewardNumber, budget * part.Weight / totalWeight, variants, assigned));
                var items = player.Items.Where(item => item.Config.SlotType == chosen.SlotType && item.Config.Rarity == chosen.Rarity).ToList();
                assigned[chosen.VariantIndex] += chosen.TotalPoints;
                var item = items[UnityEngine.Random.Range(0, items.Count)];
                result[part.RewardNumber - 1].Add(new ForecastCrystalRewardEntry
                {
                    Kind = chosen.SlotType == ItemSlotType.Weapon ? ForecastCrystalRewardKind.WeaponTier : ForecastCrystalRewardKind.ShieldTier,
                    Tier = (int)chosen.Rarity + 1,
                    Key = item.Config.Key,
                    Amount = chosen.Count
                });
            }
            return result;
        }
        private Dictionary<int, double> BuildIslandPoints()
        {
            return UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(spawner => spawner.Config.ItemReward != null && !spawner.Config.IsEvent && (int)spawner.Config.ItemReward.Rarity < RarityPoints.Length)
                .GroupBy(spawner => spawner.Config.LocationNumber)
                .ToDictionary(group => group.Key, group => group.Sum(spawner => (double)(GetRarityPoints(spawner.Config.ItemReward.Rarity) * spawner.Config.Count)));
        }
        private List<CrystalRewardVariant> BuildRewardVariants(List<ItemModel> items)
        {
            return items.Where(item => item.Config.SlotType == ItemSlotType.Weapon || item.Config.SlotType == ItemSlotType.Shield)
                .GroupBy(item => new { item.Config.SlotType, item.Config.Rarity })
                .Select(group => new CrystalRewardVariant { SlotType = group.Key.SlotType, Rarity = group.Key.Rarity })
                .OrderBy(variant => variant.SlotType).ThenBy(variant => variant.Rarity).ToList();
        }
        private List<CrystalRewardPart> GetRewardParts() => RewardSlots.SelectMany((slot, index) => slot.Select(weight => new CrystalRewardPart { RewardNumber = index + 1, Weight = weight })).ToList();
        private List<CrystalRewardCandidate> BuildCandidates(int rewardNumber, double target, List<CrystalRewardVariant> variants, double[] assigned)
        {
            var indices = Enumerable.Range(0, variants.Count).Where(index => (int)variants[index].Rarity < MinRewardNumbersByRarity.Length && GetMinRewardNumber(variants[index].Rarity) <= rewardNumber).ToList();
            double maxAssigned = indices.Max(index => assigned[index]);
            return indices.Select(index =>
            {
                int points = GetRarityPoints(variants[index].Rarity);
                int count = GetRewardCount(target, points);
                return new CrystalRewardCandidate
                {
                    VariantIndex = index, SlotType = variants[index].SlotType, Rarity = variants[index].Rarity,
                    Count = count, TotalPoints = count * points,
                    SelectWeight = Math.Pow(2.0, Math.Floor((maxAssigned - assigned[index]) / 20.0))
                };
            }).ToList();
        }
        private CrystalRewardCandidate ChooseCandidate(List<CrystalRewardCandidate> candidates)
        {
            float roll = UnityEngine.Random.Range(0f, (float)candidates.Sum(candidate => candidate.SelectWeight));
            double cumulative = 0;
            foreach (var candidate in candidates)
            {
                cumulative += candidate.SelectWeight;
                if (roll <= cumulative) return candidate;
            }
            return candidates[candidates.Count - 1];
        }
        private int GetRarityPoints(Rarity rarity) => RarityPoints[(int)rarity];
        private int GetMinRewardNumber(Rarity rarity) => MinRewardNumbersByRarity[(int)rarity];
        private int GetRewardCount(double target, int points)
        {
            // ARM64 0x14e14c4 FRINTM/FCVTMS, 0x14e14d8 FRINTP/FCVTPS; floor wins equal distance.
            int floor = Math.Max(1, (int)Math.Floor(target / points));
            int ceil = Math.Max(1, (int)Math.Ceiling(target / points));
            return Math.Abs(floor * points - target) <= Math.Abs(ceil * points - target) ? floor : ceil;
        }
    }
}
