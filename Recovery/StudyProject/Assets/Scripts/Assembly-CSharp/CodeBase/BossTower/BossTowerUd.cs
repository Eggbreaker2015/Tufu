using Multicast.UserData;

namespace Codebase.BossTower
{
	public class BossTowerUd : UdObject
	{
		public UdValue<bool> IsUnlocked { get; }

		public UdListValue<string> DefeatedBosses { get; }

		public UdValue<int> LastTicketsRestoreTimestamp { get; }

		public UdDict<BossUd> Bosses { get; }

		public BossTowerUd(UdArgs args) : base(args)
{
    IsUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_unlocked", true));
    DefeatedBosses = new global::Multicast.UserData.UdListValue<global::System.String>(Child("defeated_bosses", true));
    LastTicketsRestoreTimestamp = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("last_tickets_restore_timestamp", true));
    Bosses = new global::Multicast.UserData.UdDict<global::Codebase.BossTower.BossUd>(Child("bosses", true), childArgs => new global::Codebase.BossTower.BossUd(childArgs));
}
	}
}
