using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Notifications;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingNotificationsMasterSwitch : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003COnClick_003Ed__11 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncVoidMethodBuilder _003C_003Et__builder;

			public SettingNotificationsMasterSwitch _003C_003E4__this;

			private UniTask<bool>.Awaiter _003C_003Eu__1;

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

		private NotificationsModel model;

		private INotificationService service;

		[SerializeField]
		private Sprite offIcon;

		[SerializeField]
		private Sprite onIcon;

		[SerializeField]
		private Sprite offSwitchIcon;

		[SerializeField]
		private Sprite onSwitchIcon;

		[SerializeField]
		private Image image;

		[SerializeField]
		private Image switchImage;

		[SerializeField]
		private Button button;

		private float offset;

		private void Awake()
		{
		}

		[AsyncStateMachine(typeof(_003COnClick_003Ed__11))]
		private void OnClick()
		{
		}

		private void Render(bool value)
		{
		}
	}
}
