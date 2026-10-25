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

		public OfferProductHandler(ProductKey productKey, Sprite icon, Func<RewardBase> rewardFactory)
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
