using Multicast.UserData;

namespace Codebase.Cashback
{
	public class CashbackUd : UdObject
	{
		public UdValue<int> PeriodId { get; }

		public UdListObject<CashbackStatEntryUd> EarnedStats { get; }

		public CashbackUd(UdArgs args) : base(args)
{
    PeriodId = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("period_id", true));
    EarnedStats = new global::Multicast.UserData.UdListObject<global::Codebase.Cashback.CashbackStatEntryUd>(Child("earned_stats", true), childArgs => new global::Codebase.Cashback.CashbackStatEntryUd(childArgs));
}
	}
}
