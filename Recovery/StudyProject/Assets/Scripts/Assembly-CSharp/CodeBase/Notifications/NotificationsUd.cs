using Multicast.UserData;

namespace Codebase.Notifications
{
	public class NotificationsUd : UdObject
	{
		private readonly UdDict<NotificationUd> byKey;

		public UdValue<bool> MasterEnabled { get; }

		public NotificationsUd(UdArgs args) : base(args)
{
    MasterEnabled = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("master_enabled", true), true);
    byKey = new global::Multicast.UserData.UdDict<global::Codebase.Notifications.NotificationUd>(Child("by_key", true), childArgs => new global::Codebase.Notifications.NotificationUd(childArgs));
}

		public NotificationUd Get(string key)
		{
			return null;
		}
	}
}
