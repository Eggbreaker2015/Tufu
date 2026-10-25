using Cysharp.Threading.Tasks;

namespace Codebase.Notifications
{
	public interface INotificationService
	{
		bool IsPermissionGranted { get; }

		bool IsPermissionPermanentlyDenied { get; }

		void Initialize();

		UniTask<bool> RequestPermission();

		int Schedule(NotificationRequest request);

		void Cancel(int osId);

		void CancelAll();

		string GetLaunchNotificationKey();

		void OpenOsSettings();
	}
}
