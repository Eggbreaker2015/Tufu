using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.UI.Amulet;
using Codebase.UI.Flows;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.Amulet.Flows
{
	public class FocusGiantSoulFlowRunner : IFlowRunner<FocusGiantSoulFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAnimateCameraToTarget_003Ed__6 : IAsyncStateMachine
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

			public FocusGiantSoulFlowRunner _003C_003E4__this;

			public FocusGiantSoulFlow flow;

			private EnemyUnit _003Cenemy_003E5__2;

			private GiantSoulObjectiveView _003Cbanner_003E5__3;

			private Transform _003Canchor_003E5__4;

			private UniTask.Awaiter _003C_003Eu__1;

			private ItemSlotUnlockFlow? _003CunlockFlow_003E5__5;

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

		private const float FlightDuration = 1f;

		private const float FocusHoldDuration = 2f;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		[Inject]
		private EnemiesModel enemiesModel;

		[AsyncStateMachine(typeof(_003CExecute_003Ed__5))]
		public UniTask Execute(FocusGiantSoulFlow flow)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CAnimateCameraToTarget_003Ed__6))]
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
