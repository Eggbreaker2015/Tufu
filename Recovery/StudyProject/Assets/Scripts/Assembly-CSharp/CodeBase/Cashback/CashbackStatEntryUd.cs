using Multicast.UserData;

namespace Codebase.Cashback
{
	public class CashbackStatEntryUd : UdObject
	{
		public UdValue<int> StatType { get; }

		public UdValue<double> Value { get; }

		public CashbackStatEntryUd(UdArgs args) : base(args)
{
    StatType = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("stat_type", true));
    Value = new global::Multicast.UserData.UdValue<global::System.Double>(Child("value", true));
}
	}
}
