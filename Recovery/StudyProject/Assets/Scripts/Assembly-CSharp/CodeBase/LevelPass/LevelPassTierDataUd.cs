using Multicast.UserData;

namespace Codebase.LevelPass
{
	public class LevelPassTierDataUd : UdObject
	{
		public UdValue<int> Progress { get; }

		public UdValue<bool> FreeRewardClaimed { get; }

		public UdValue<bool> PaidMultipliedRewardClaimed { get; }

		public UdValue<bool> PaidRewardClaimed { get; }

		public LevelPassTierDataUd(UdArgs args) : base(args)
{
    Progress = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("progress", true));
    FreeRewardClaimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("free_claimed", true));
    PaidMultipliedRewardClaimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("paid_multiplied_claimed", true));
    PaidRewardClaimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("paid_claimed", true));
}
	}
}
