using Multicast.UserData;

namespace Codebase.VIP
{
	public class VipUd : UdObject
	{
		public UdValue<int> DailyChestCooldownTimestamp { get; }

		public UdValue<int> AutoFarmCooldownTimestamp { get; }

		public VipUd(UdArgs args) : base(args)
{
    DailyChestCooldownTimestamp = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("daily_chest_cooldown_timestamp", true));
    AutoFarmCooldownTimestamp = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("auto_farm_cooldown_timestamp", true));
}
	}
}
