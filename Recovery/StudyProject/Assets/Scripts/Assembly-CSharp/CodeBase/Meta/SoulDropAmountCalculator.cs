using System;
namespace Codebase.Meta
{
    public static class SoulDropAmountCalculator
    {
        // Original ELF VA 0x1488810; per-kill clamps only the multiplier, not the rarity bonus.
        public static double CalculatePerKill(string soulKey, double rewardMultiplier, MetaStatsModel metaStatsModel)
            => (Math.Max(1.0, GetDropMultiplier(metaStatsModel)) + GetRarityFlatBonus(metaStatsModel,soulKey)) * rewardMultiplier;
        // Original ELF VA 0x14889a4 deliberately has no per-kill clamp.
        public static double CalculateForKillCount(string soulKey, double killCount, MetaStatsModel metaStatsModel)
            => (GetDropMultiplier(metaStatsModel) + GetRarityFlatBonus(metaStatsModel,soulKey)) * killCount;
        private static double GetDropMultiplier(MetaStatsModel metaStatsModel) => 1.0 + metaStatsModel.Get("soul_drop_bonus").GetValue(0);
        private static double GetRarityFlatBonus(MetaStatsModel metaStatsModel, string soulKey) => metaStatsModel.Get("souls_rarity_amount").GetValue(soulKey);
    }
}
