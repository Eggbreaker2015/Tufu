using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using System;
using Codebase.UI.BossTower;

namespace Codebase.Gates.Flows
{
	public class PortalUnlockFlowRunner : IFlowRunner<PortalUnlockFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__8 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public PortalUnlockFlowRunner _003C_003E4__this;
			public PortalUnlockFlow flow;
			private UniTask.Awaiter _003C_003Eu__1;
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

		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		[Inject]
		private Portal portal;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private UiModel uiModel;
		private const float CameraFocusDelay = 1.5f;
		private const float CameraFastFocusDelay = 0.5f;
		private const float CameraDistance = 7f;
		public async UniTask Execute(PortalUnlockFlow flow)
		{
			playerModel.CanMove = false;
			playerModel.IsPortalUnlockRunning = true;
			uiModel.IsHudVisible = false;
			cameraFollowSystem.Follow(portal.transform, 7f, -20f, true, 1.5f);
			await UniTask.Delay(TimeSpan.FromSeconds(1.5));
			await portal.PlayUnlockAnimation();
			await UniTask.WaitForSeconds(1f);
			await navigator.Open<BossTowerPortalUnlockView>("Assets/Prefabs/UI/BossTower/BossTowerPortalUnlockView.prefab").WaitForContinue();
			playerModel.IsUnlockAnimationShown = true;
			playerModel.CanMove = true;
			if (flow.FocusFullTime)
				cameraFollowSystem.Unfollow();
			playerModel.IsPortalUnlockRunning = false;
			portal.CleanupUnlockAnimation();
			uiModel.IsHudVisible = true;
		}
	}
}
