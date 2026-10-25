using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Rewards;
using Codebase.UI.Shop;

namespace Codebase.Services.Purchases.Handlers
{
	public class GemsProductHandler : IProductHandler
	{
		private readonly Dictionary<ProductKey, ShopLotDef> products;

		public GemsProductHandler RegisterFromConfig(ShopSectionsConfig shopConfig)
		{
			return null;
		}

		public bool CanHandle(ProductKey productKey)
		{
			return false;
		}

		public void BuildRewards(List<RewardBase> rewards, ProductKey productKey)
		{
		}

		public PurchaseRewardInfo? GetRewardInfo(ProductKey productKey, int quantity)
		{
			return null;
		}
	}
}
