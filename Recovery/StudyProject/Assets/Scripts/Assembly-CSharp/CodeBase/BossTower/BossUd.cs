using Multicast.UserData;

namespace Codebase.BossTower
{
	public class BossUd : UdObject
	{
		public UdValue<int> Floor;

		public UdValue<int> DailyClaims;

		public UdValue<bool> IsWeakened;

		public BossUd(UdArgs args) : base(args)
{
    Floor = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("floor", true), 1);
    DailyClaims = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("daily_claims", true), 0);
    IsWeakened = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_weakened", true), false);
}
	}
}
