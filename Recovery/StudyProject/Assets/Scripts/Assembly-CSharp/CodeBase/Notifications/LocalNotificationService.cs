using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Unity.Notifications.Android;

namespace Codebase.Notifications
{
	public class LocalNotificationService : INotificationService
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CRequestPermission_003Ed__11 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<bool> _003C_003Et__builder;

			public LocalNotificationService _003C_003E4__this;

			private PermissionRequest _003Crequest_003E5__2;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		private int nextId;

		private string launchKey;

		public bool IsPermissionGranted { get; private set; }

		public bool IsPermissionPermanentlyDenied { get; private set; }

		public void Initialize()
		{
		}

		[AsyncStateMachine(typeof(_003CRequestPermission_003Ed__11))]
		public UniTask<bool> RequestPermission()
		{
			return default;
		}

		public int Schedule(NotificationRequest request)
		{
			return 0;
		}

		public void Cancel(int osId)
		{
		}

		public void CancelAll()
		{
		}

		public string GetLaunchNotificationKey()
		{
			return this.launchKey;
		}

		public void OpenOsSettings()
		{
		}

		[Conditional("DEVELOP_ENABLED")]
		private static void Log(string message)
		{
		}
	}
}
