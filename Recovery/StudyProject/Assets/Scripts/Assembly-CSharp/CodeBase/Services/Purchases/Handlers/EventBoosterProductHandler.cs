using System.Collections.Generic;
using Codebase.Events;
using Codebase.In_apps;
using Codebase.Rewards;

namespace Codebase.Services.Purchases.Handlers
{
    public class EventBoosterProductHandler : IProductHandler
    {
        private IAPManager _iapManager;
        private readonly Dictionary<ProductKey, EventBoosterDef> products = new Dictionary<ProductKey, EventBoosterDef>();

        // Original ARM64 ELF VA 0x1406814, 0x14097e8, 0x1409854, 0x140993c.
        public EventBoosterProductHandler RegisterFromConfig(EventsProvider eventsProvider, IAPManager iapManager)
        {
            _iapManager = iapManager;
            foreach (var config in eventsProvider.Events)
                if (config.Boosters != null)
                    foreach (var booster in config.Boosters)
                        if (booster.Type == EventBoosterType.IAP && booster.ProductKey != ProductKey.None)
                            products[booster.ProductKey] = booster;
            return this;
        }
        public bool CanHandle(ProductKey productKey) => products.ContainsKey(productKey);
        public void BuildRewards(List<RewardBase> rewards, ProductKey productKey)
        {
            if (products.ContainsKey(productKey))
                rewards.Add(new FreeBoosterTokenReward { productId = _iapManager.GetStoreId(productKey) });
        }
        public PurchaseRewardInfo? GetRewardInfo(ProductKey productKey, int quantity)
        {
            if (!products.TryGetValue(productKey, out var booster)) return null;
            return new PurchaseRewardInfo(booster.TokenSprite, string.Format("x{0}", quantity), productKey, quantity);
        }
    }
}
