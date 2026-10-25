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

		private readonly HashSet<ProductKey> productKeys;

		private Sprite icon;

		public LevelPassProductHandler RegisterFromConfig(LevelPassConfig config, IAPManager iapManager, Sprite icon)
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
