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

		public NoAdsProductHandler(ProductKey productKey, Sprite icon)
		{
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
