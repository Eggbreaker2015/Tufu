using System;
using System.Collections.Generic;

namespace Codebase.Notifications
{
	public interface INotificationSchedule
	{
		IEnumerable<NotificationRequest> Build(DateTime nowUtc);
	}
}
