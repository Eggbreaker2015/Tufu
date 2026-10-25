using Multicast.UserData;

namespace Codebase.PromoCodes
{
	public class PromoCodesUd : UdObject
	{
		public UdListValue<string> UsedCodes { get; }

		public UdValue<bool> HashRewardClaimed { get; }

		public PromoCodesUd(UdArgs args) : base(args)
{
    UsedCodes = new global::Multicast.UserData.UdListValue<global::System.String>(Child("used_codes", true));
    HashRewardClaimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("hash_reward_claimed", true));
}
	}
}
