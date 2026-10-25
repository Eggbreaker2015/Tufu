using Multicast.UserData;

namespace Codebase.Player.DailyProcs
{
	public class DailyProcsUd : UdObject
	{
		public UdValue<long> LastResetDayTicks { get; }

		public UdDictValue<int> Counts { get; }

		public DailyProcsUd(UdArgs args) : base(args)
{
    LastResetDayTicks = new global::Multicast.UserData.UdValue<global::System.Int64>(Child("last_reset_day", true));
    Counts = new global::Multicast.UserData.UdDictValue<global::System.Int32>(Child("counts", true));
}
	}
}
