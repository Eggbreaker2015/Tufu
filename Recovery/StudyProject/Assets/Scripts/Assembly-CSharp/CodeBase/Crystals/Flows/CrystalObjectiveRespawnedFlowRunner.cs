using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Crystals.Objective;
using Codebase.Enemy;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using System;
using System.Linq;

namespace Codebase.Crystals.Flows
{
	public class CrystalObjectiveRespawnedFlowRunner : IFlowRunner<CrystalObjectiveRespawnedFlow>, IFlowHandlerBase
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
			public CrystalObjectiveRespawnedFlowRunner _003C_003E4__this;
			private CrystalUnit _003Ccrystal_003E5__2;
			private Transform _003Canchor_003E5__3;
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

		private const float FlightDuration = 1f;
		private const float FocusHoldDuration = 2f;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		[Inject]
		private EnemiesModel enemiesModel;
		public async UniTask Execute(CrystalObjectiveRespawnedFlow flow)
		{
			var crystal = enemiesModel.Enemies.OfType<CrystalUnit>().FirstOrDefault(unit => !unit.IsDead);
			if (crystal == null)
				return;
			playerModel.CanMove = false;
			var anchor = new GameObject("CrystalObjectiveRespawnCameraAnchor").transform;
			anchor.position = playerModel.Unit.transform.position;
			cameraFollowSystem.Follow(anchor, 0f, 0f, false, 0f);
			await AnimateCameraToTarget(anchor, crystal.transform);
			anchor.SetParent(crystal.transform);
			anchor.localPosition = Vector3.zero;
			await UniTask.Delay(TimeSpan.FromSeconds(2.0), false, PlayerLoopTiming.Update);
			cameraFollowSystem.Unfollow();
			UnityEngine.Object.Destroy(anchor.gameObject);
			playerModel.CanMove = true;
		}

		private async UniTask AnimateCameraToTarget(Transform anchor, Transform target)
		{
			Vector3 startPosition = anchor.position;
			float elapsed = 0f;
			do
			{
				elapsed += Time.deltaTime;
				anchor.position = Vector3.Lerp(startPosition, target.position, EaseOutQuad(Mathf.Clamp01(elapsed)));
				await UniTask.Yield();
			}
			while (elapsed < 1f);
			anchor.position = target.position;
		}

		private static float EaseOutQuad(float t)
		{
			return 1f - (1f - t) * (1f - t);
		}
	}
}
