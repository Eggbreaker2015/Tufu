using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Rewards;

namespace Codebase.Services.Purchases.Handlers
{
	public interface IProductHandler
	{
		bool CanHandle(ProductKey productKey);

		PurchaseRewardInfo? GetRewardInfo(ProductKey productKey, int quantity);

		void BuildRewards(List<RewardBase> rewards, ProductKey productKey);
	}
}
