using System;
using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Services.Purchases.Handlers
{
    public class OfferProductHandler : IProductHandler
    {
        private readonly ProductKey productKey;
        private readonly Sprite icon;
        private readonly Func<RewardBase> rewardFactory;

        // Original ARM64 ELF VA 0x1406d9c, 0x140a0d4, 0x140a0e4, 0x140a164.
        public OfferProductHandler(ProductKey productKey, Sprite icon, Func<RewardBase> rewardFactory)
        {
            this.productKey = productKey;
            this.icon = icon;
            this.rewardFactory = rewardFactory;
        }
        public bool CanHandle(ProductKey key) => key == productKey;
        public void BuildRewards(List<RewardBase> rewards, ProductKey key) => rewards.Add(rewardFactory());
        public PurchaseRewardInfo? GetRewardInfo(ProductKey key, int quantity) => new PurchaseRewardInfo(icon, string.Empty, key, quantity);
    }
}
