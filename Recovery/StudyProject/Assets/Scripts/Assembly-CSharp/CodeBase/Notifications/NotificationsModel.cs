using System.Collections.Generic;
using Codebase.Core;
using UniMob;
using UniMob.Core;

namespace Codebase.Notifications
{
	public class NotificationsModel : Model<NotificationsUd>
	{
		[Inject]
		private INotificationService service;

		private List<INotificationSchedule> schedules;

		private ComputedAtom<bool> __MasterEnabled;

		public bool MasterEnabled
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public NotificationsModel(Lifetime lifetime)
			: base(default(Lifetime))
		{
		}

		public bool IsKeyEnabled(string key)
		{
			return false;
		}

		public void SetKeyEnabled(string key, bool value)
		{
		}

		public void InitializeNotifications()
		{
		}

		public void CancelAll()
		{
		}

		public void RescheduleAll()
		{
		}

		private List<NotificationRequest> CollectRequests()
		{
			return null;
		}
	}
}
