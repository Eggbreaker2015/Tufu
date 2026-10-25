using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Rewards;
using Codebase.UI.Shop;
using Codebase.Utilities;

namespace Codebase.Services.Purchases.Handlers
{
    public class GemsProductHandler : IProductHandler
    {
        private readonly Dictionary<ProductKey, ShopLotDef> products = new Dictionary<ProductKey, ShopLotDef>();

        // Original ARM64 ELF VA 0x1406328, 0x1409ad4, 0x1409b40, 0x1409c24.
        public GemsProductHandler RegisterFromConfig(ShopSectionsConfig shopConfig)
        {
            foreach (var section in shopConfig.Sections)
                foreach (var lot in section.Lots)
                    if (lot.LotType == ShopLotType.Gems && lot.ProductKey != ProductKey.None)
                        products[lot.ProductKey] = lot;
            return this;
        }

        public bool CanHandle(ProductKey productKey) => products.ContainsKey(productKey);

        public void BuildRewards(List<RewardBase> rewards, ProductKey productKey)
        {
            if (products.TryGetValue(productKey, out var lot))
                rewards.Add(new GemsReward { Gems = lot.Value });
        }

        public PurchaseRewardInfo? GetRewardInfo(ProductKey productKey, int quantity)
        {
            if (!products.TryGetValue(productKey, out var lot)) return null;
            return new PurchaseRewardInfo(lot.Sprite, (lot.Value * quantity).Format(), productKey, quantity);
        }
    }
}
