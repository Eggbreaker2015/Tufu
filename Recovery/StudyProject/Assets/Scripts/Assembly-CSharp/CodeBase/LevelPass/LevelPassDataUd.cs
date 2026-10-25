using Multicast.UserData;

namespace Codebase.LevelPass
{
	public class LevelPassDataUd : UdObject
	{
		public UdValue<string> Key { get; }

		public UdValue<bool> Purchased { get; }

		public UdListObject<LevelPassTierDataUd> Tiers { get; }

		public LevelPassDataUd(UdArgs args) : base(args)
{
    Key = new global::Multicast.UserData.UdValue<global::System.String>(Child("key", true));
    Purchased = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("purchased", true));
    Tiers = new global::Multicast.UserData.UdListObject<global::Codebase.LevelPass.LevelPassTierDataUd>(Child("tiers", true), childArgs => new global::Codebase.LevelPass.LevelPassTierDataUd(childArgs));
}
	}
}
