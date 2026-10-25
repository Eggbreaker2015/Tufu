using System;
using Codebase.Enemy;
using Codebase.Player.Inventory;

namespace Codebase.Meta
{
    public static class SpawnerTimerCalculator
    {
        public static float Apply(float seconds, EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
        {
            if (seconds <= 0f || metaStatsModel == null) return seconds;
            if (config.ItemReward != null) return ApplyItemDivisor(seconds, config, metaStatsModel);
            if (!config.IsEvent) return ApplyStatSpawnerReduction(seconds, config, metaStatsModel);
            return seconds;
        }
        public static double GetStatSpawnerTimerReduction(EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
        {
            if (config.ItemReward != null || metaStatsModel == null || config.IsEvent) return 0.0;
            return metaStatsModel.Get("map_spawner_timer_reduce").GetValue(config.LocationNumber);
        }
        private static float ApplyItemDivisor(float seconds, EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
            => (float)(seconds / Math.Max(1.0, metaStatsModel.Get("items_rarity_timer").GetValue(RarityStatParam.From(config.ItemReward.Rarity))));
        private static float ApplyStatSpawnerReduction(float seconds, EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
            => Math.Max(0f, seconds - (float)GetStatSpawnerTimerReduction(config, metaStatsModel));
    }
}
