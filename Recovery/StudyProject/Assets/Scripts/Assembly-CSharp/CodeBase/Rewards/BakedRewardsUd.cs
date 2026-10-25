using Multicast.UserData;

namespace Codebase.Rewards
{
	public class BakedRewardsUd : UdObject
	{
		public UdDict<BakedStatRewardUd> Stats { get; }

		public UdDict<BakedItemRewardUd> Items { get; }

		public BakedRewardsUd(UdArgs args) : base(args)
{
    Stats = new global::Multicast.UserData.UdDict<global::Codebase.Rewards.BakedStatRewardUd>(Child("baked_stats", true), childArgs => new global::Codebase.Rewards.BakedStatRewardUd(childArgs));
    Items = new global::Multicast.UserData.UdDict<global::Codebase.Rewards.BakedItemRewardUd>(Child("baked_items", true), childArgs => new global::Codebase.Rewards.BakedItemRewardUd(childArgs));
}
	}
}
