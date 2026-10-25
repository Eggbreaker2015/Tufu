namespace Codebase.Rewards
{
	public static class BakedRewardStorage
	{
		private static BakedRewardsUd Ud
		{
			get
			{
				return Codebase.Core.UserData.UserDataProvider.Resolve<BakedRewardsUd>();
			}
		}

		public static void Save(string key, BakedStatReward bakedReward)
		{
			var data = Ud.Stats.GetOrCreate(key, out bool created);
			data.StatType.Value = bakedReward.Type;
			data.Value.Value = bakedReward.Value;
		}

		public static BakedStatReward Load(string key)
		{
			if (!Ud.Stats.ContainsKey(key))
				return null;
			var data = Ud.Stats[key];
			return new BakedStatReward
			{
				Type = data.StatType.Value,
				Value = data.Value.Value
			};
		}

		public static bool Has(string key)
		{
			return Ud.Stats.ContainsKey(key);
		}

		public static void Remove(string key)
		{
			Ud.Stats.Remove(key);
		}

		public static void SaveItem(string key, BakedItemReward bakedReward)
		{
			var data = Ud.Items.GetOrCreate(key, out bool created);
			data.ItemKey.Value = bakedReward.ItemKey;
		}

		public static BakedItemReward LoadItem(string key)
		{
			if (!Ud.Items.ContainsKey(key))
				return null;
			return new BakedItemReward
			{
				ItemKey = Ud.Items[key].ItemKey.Value
			};
		}

		public static bool HasItem(string key)
		{
			return Ud.Items.ContainsKey(key);
		}

		public static void RemoveItem(string key)
		{
			Ud.Items.Remove(key);
		}
	}
}
