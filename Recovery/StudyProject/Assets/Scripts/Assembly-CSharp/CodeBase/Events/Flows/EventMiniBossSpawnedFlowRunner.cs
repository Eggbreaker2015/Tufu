using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.Events.Flows
{
	public class EventMiniBossSpawnedFlowRunner : IFlowRunner<EventMiniBossSpawnedFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAnimateCameraToTarget_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public Transform anchor;

			public Transform target;

			private Vector3 _003CstartPosition_003E5__2;

			private float _003Celapsed_003E5__3;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__5 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventMiniBossSpawnedFlowRunner _003C_003E4__this;

			public EventMiniBossSpawnedFlow flow;

			private Transform _003CcameraAnchor_003E5__2;

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

		private const float CameraHeight = 15f;

		private const float CameraOffset = -3f;

		private const float FlightDuration = 1f;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		[AsyncStateMachine(typeof(_003CExecute_003Ed__5))]
		public UniTask Execute(EventMiniBossSpawnedFlow flow)
		{
			return default;
		}

		private Transform CreateCameraAnchor()
		{
			return null;
		}

		[AsyncStateMachine(typeof(_003CAnimateCameraToTarget_003Ed__7))]
		private UniTask AnimateCameraToTarget(Transform anchor, Transform target)
		{
			return default;
		}

		private static float EaseOutQuad(float t)
		{
			return 0f;
		}
	}
}
