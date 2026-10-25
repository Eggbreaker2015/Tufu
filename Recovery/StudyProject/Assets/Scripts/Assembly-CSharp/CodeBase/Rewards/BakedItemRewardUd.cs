using Multicast.UserData;

namespace Codebase.Rewards
{
	public class BakedItemRewardUd : UdObject
	{
		public UdValue<string> ItemKey { get; }

		public BakedItemRewardUd(UdArgs args) : base(args)
{
    ItemKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("item_key", true), string.Empty);
}
	}
}
