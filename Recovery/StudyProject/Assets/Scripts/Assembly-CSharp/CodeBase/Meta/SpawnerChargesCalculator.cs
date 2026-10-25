using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using UnityEngine;

namespace Codebase.Meta
{
    public static class SpawnerChargesCalculator
    {
        private readonly struct CachedBonusCharges
        {
            public readonly int ModifierVersion;
            public readonly int Value;
            public CachedBonusCharges(int modifierVersion, int value)
            {
                ModifierVersion = modifierVersion;
                Value = value;
            }
        }
        private static readonly Dictionary<(MetaStat Stat, int RarityParam), CachedBonusCharges> cachedBonusCharges = new Dictionary<(MetaStat, int), CachedBonusCharges>();
        private static int cachedBonusChargesFrame = -1;
        public static int GetBonusCharges(EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
        {
            if (config.ItemReward == null) return 0;
            int frame = Time.frameCount;
            if (cachedBonusChargesFrame != frame)
            {
                cachedBonusCharges.Clear();
                cachedBonusChargesFrame = frame;
            }
            MetaStat stat = metaStatsModel.Get("items_rarity_charges");
            int rarityParam = RarityStatParam.From(config.ItemReward.Rarity);
            var key = (stat, rarityParam);
            if (cachedBonusCharges.TryGetValue(key, out var cached) && cached.ModifierVersion == stat.ModifierVersion) return cached.Value;
            int bonus = (int)stat.GetValue(rarityParam);
            cachedBonusCharges[key] = new CachedBonusCharges(stat.ModifierVersion, bonus);
            return bonus;
        }
        public static int GetTotalCharges(EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
            => config.ChargesBeforeBigRespawn + GetBonusCharges(config, metaStatsModel);
        [StaticReset]
        private static void ResetStaticState()
        {
            cachedBonusCharges.Clear();
            cachedBonusChargesFrame = -1;
        }
    }
}
