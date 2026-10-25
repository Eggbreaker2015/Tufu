using System.Collections.Generic;
using Codebase.Events;
using Codebase.In_apps;
using Codebase.Rewards;

namespace Codebase.Services.Purchases.Handlers
{
	public class EventBoosterProductHandler : IProductHandler
	{
		private IAPManager _iapManager;

		private readonly Dictionary<ProductKey, EventBoosterDef> products;

		public EventBoosterProductHandler RegisterFromConfig(EventsProvider eventsProvider, IAPManager iapManager)
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
