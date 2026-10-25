using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Multicast.Backend;

namespace Codebase.Hub
{
	public class HubRewardParser
	{
		public static readonly HubRewardParser Instance;

		[Inject]
		private ItemsProvider itemsProvider;

		public RewardBase[] FromHubRewards(BackendApi.IRewardItem[] rewards)
		{
			return null;
		}

		public RewardBase FromHubReward(BackendApi.IRewardItem reward)
		{
			return null;
		}

		private GemsReward CreateGemsReward(double amount)
		{
			return null;
		}

		private ItemReward CreateItemReward(string itemKey, string levelStr)
		{
			return null;
		}

		private EventCurrencyReward CreateEventCurrencyReward(string key, double amount)
		{
			return null;
		}

		private CurrencyReward CreateCurrencyReward(string part1, string part2, double amount)
		{
			return null;
		}

		private EventReward CreateEventReward(int extraDurationMinutes)
		{
			return null;
		}

		private StatReward CreateStatReward(string statName, float value)
		{
			return null;
		}

		private ChestReward CreateChestReward(string key, int amount)
		{
			return null;
		}

		private VipPointsReward CreateVipPointsReward(int amount)
		{
			return null;
		}

		private BundleReward CreateBundleReward(string bundleName, BackendApi.IRewardItem[] items)
		{
			return null;
		}

		public RewardBase FromSkuString(string skuString)
		{
			return null;
		}

		public static (string, double) DestructSkuString(string skuString)
		{
			return default;
		}

		public static string ToSkuString(RewardBase reward)
		{
			return null;
		}

		public static void DestructSku(string sku, out string type, out string detail1, out string detail2, out string detail3)
		{
			type = null;
			detail1 = null;
			detail2 = null;
			detail3 = null;
		}

		private static string ExtractProductId(string sku, string type)
		{
			return null;
		}
	}
}
