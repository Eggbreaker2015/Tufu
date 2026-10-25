using System;

namespace Codebase.Notifications
{
	public readonly struct NotificationRequest
	{
		public readonly string InternalKey;

		public readonly string Title;

		public readonly string Body;

		public readonly DateTime FireTimeUtc;

		public NotificationRequest(string internalKey, string title, string body, DateTime fireTimeUtc)
		{
			InternalKey = null;
			Title = null;
			Body = null;
			FireTimeUtc = default;
		}
	}
}
