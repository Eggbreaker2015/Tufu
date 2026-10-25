using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Events.Models;
using Codebase.Events.Systems;
using Codebase.Player.Models;
using Codebase.UI.Events;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.Events.Flows
{
	public class EventMiniBossKilledFlowRunner : IFlowRunner<EventMiniBossKilledFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventMiniBossKilledFlowRunner _003C_003E4__this;

			public EventMiniBossKilledFlow flow;

			private EventModel _003CcurrentEvent_003E5__2;

			private EventBossGate _003Cgate_003E5__3;

			private BossHuntProgressView _003CprogressView_003E5__4;

			private Transform _003CcameraAnchor_003E5__5;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CHandleGateOpening_003Ed__10 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossHuntProgressView progressView;

			public EventBossGate gate;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CHandleProgressAnimation_003Ed__9 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossHuntProgressView progressView;

			public int progressIndex;

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

		private const float WaitAfterProgressAnimation = 3f;

		private const float WaitAfterGateOpen = 3f;

		private const float CameraFollowDistance = 0f;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		[Inject]
		private EventBossGateSystem eventBossGateSystem;

		[AsyncStateMachine(typeof(_003CExecute_003Ed__7))]
		public UniTask Execute(EventMiniBossKilledFlow flow)
		{
			return default;
		}

		private Transform CreateCameraAnchor(Transform gateAnchor)
		{
			return null;
		}

		[AsyncStateMachine(typeof(_003CHandleProgressAnimation_003Ed__9))]
		private UniTask HandleProgressAnimation(BossHuntProgressView progressView, int progressIndex)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CHandleGateOpening_003Ed__10))]
		private UniTask HandleGateOpening(EventBossGate gate, BossHuntProgressView progressView)
		{
			return default;
		}
	}
}
