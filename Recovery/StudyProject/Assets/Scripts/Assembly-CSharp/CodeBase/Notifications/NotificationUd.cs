using Multicast.UserData;

namespace Codebase.Notifications
{
	public class NotificationUd : UdObject
	{
		public UdValue<bool> Enabled { get; }

		public UdValue<int> ScheduledOsId { get; }

		public NotificationUd(UdArgs args) : base(args)
{
    Enabled = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("enabled", true), true);
    ScheduledOsId = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("scheduled_os_id", true));
}
	}
}
