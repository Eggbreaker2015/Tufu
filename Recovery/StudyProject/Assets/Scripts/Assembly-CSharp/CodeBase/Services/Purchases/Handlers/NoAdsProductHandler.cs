using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Services.Purchases.Handlers
{
    public class NoAdsProductHandler : IProductHandler
    {
        private readonly ProductKey productKey;
        private readonly Sprite icon;

        // Original ARM64 ELF VA 0x1406d64, 0x1409f7c, 0x1409f8c, 0x140a008.
        public NoAdsProductHandler(ProductKey productKey, Sprite icon)
        {
            this.productKey = productKey;
            this.icon = icon;
        }
        public bool CanHandle(ProductKey key) => key == productKey;
        public void BuildRewards(List<RewardBase> rewards, ProductKey key) => rewards.Add(new NoAdsReward());
        public PurchaseRewardInfo? GetRewardInfo(ProductKey key, int quantity) => new PurchaseRewardInfo(icon, string.Empty, key, quantity);
    }
}
