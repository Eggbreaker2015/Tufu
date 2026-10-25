using Multicast.UserData;

namespace Codebase.Shop
{
	public class ShopUd : UdObject
	{
		public UdDictValue<bool> ClaimedLots { get; }

		public UdValue<string> NextFreeClaimReset { get; }

		public UdDictValue<string> FreeChestClaimReset { get; }

		public ShopUd(UdArgs args) : base(args)
{
    ClaimedLots = new global::Multicast.UserData.UdDictValue<global::System.Boolean>(Child("claimed_lots", true));
    NextFreeClaimReset = new global::Multicast.UserData.UdValue<global::System.String>(Child("next_free_claim_reset", true));
    FreeChestClaimReset = new global::Multicast.UserData.UdDictValue<global::System.String>(Child("free_chest_claim_reset", true));
}
	}
}
