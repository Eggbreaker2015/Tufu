using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.LevelPass;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Services.Purchases.Handlers
{
    public class LevelPassProductHandler : IProductHandler
    {
        private IAPManager _iapManager;
        private readonly HashSet<ProductKey> productKeys = new HashSet<ProductKey>();
        private Sprite icon;

        // Original ARM64 ELF VA 0x1406e78, 0x1409d90, 0x1409dfc, 0x1409eb0.
        public LevelPassProductHandler RegisterFromConfig(LevelPassConfig config, IAPManager iapManager, Sprite icon)
        {
            _iapManager = iapManager;
            this.icon = icon;
            foreach (var pass in config.Passes)
                if (pass.ProductKey != ProductKey.None) productKeys.Add(pass.ProductKey);
            return this;
        }
        public bool CanHandle(ProductKey productKey) => productKeys.Contains(productKey);
        public void BuildRewards(List<RewardBase> rewards, ProductKey productKey) => rewards.Add(new LevelPassReward { productId = _iapManager.GetStoreId(productKey) });
        public PurchaseRewardInfo? GetRewardInfo(ProductKey productKey, int quantity) => new PurchaseRewardInfo(icon, string.Empty, productKey, quantity);
    }
}
