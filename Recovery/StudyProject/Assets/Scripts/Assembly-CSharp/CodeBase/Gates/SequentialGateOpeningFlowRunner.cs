using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Player.Models;
using Codebase.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using System;
using DG.Tweening;

namespace Codebase.Gates
{
	public class SequentialGateOpeningFlowRunner : IFlowRunner<SequentialGateOpeningFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public SequentialGateOpeningFlow flow;
			public SequentialGateOpeningFlowRunner _003C_003E4__this;
			private UniTask.Awaiter _003C_003Eu__1;
			private List<Gate>.Enumerator _003C_003E7__wrap1;
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
		private struct _003CHandleGateOpening_003Ed__4 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public bool isFirst;
			public SequentialGateOpeningFlowRunner _003C_003E4__this;
			public Gate gate;
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

		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		[Inject]
		private UiModel uiModel;
		public async UniTask Execute(SequentialGateOpeningFlow flow)
		{
			if (flow.Gates == null || flow.Gates.Count == 0)
			{
				uiModel.IsGateOpeningShowing = false;
				return;
			}

			try
			{
				await UniTask.WaitUntil(() => !uiModel.IsItemSlotUnlockShowing);
				cameraFollowSystem.ShowingGate = true;
				playerModel.CanMove = false;
				bool isFirst = true;
				foreach (Gate gate in flow.Gates)
				{
					await HandleGateOpening(gate, isFirst);
					isFirst = false;
				}

				await UniTask.Delay(TimeSpan.FromSeconds(0.5));
				cameraFollowSystem.Unfollow();
				cameraFollowSystem.ShowingGate = false;
				if (!uiModel.IsPortalGemCollectShowing)
					playerModel.CanMove = true;
			}
			finally
			{
				uiModel.IsGateOpeningShowing = false;
			}
		}

		private async UniTask HandleGateOpening(Gate gate, bool isFirst)
		{
			float cap = isFirst ? 2f : 1f;
			Transform cameraAnchor = CreateCameraAnchor(gate.transform);
			cameraFollowSystem.Follow(cameraAnchor, 0f);
			float distance = Vector3.Distance(gate.transform.position, playerModel.Unit.transform.position);
			float duration = Mathf.Max(0.25f, Mathf.Max(distance / 120f, Mathf.Min(distance / 30f, cap)));
			cameraAnchor.DOMove(gate.transform.position + new Vector3(0f, 15f, -3f), duration).SetEase(Ease.OutQuad);
			await UniTask.Delay(TimeSpan.FromSeconds((double)duration - 0.25));
			gate.Open();
			await UniTask.Delay(TimeSpan.FromSeconds(2.25));
			UnityEngine.Object.Destroy(cameraAnchor.gameObject, 1f);
		}

		private Transform CreateCameraAnchor(Transform gateTransform)
		{
			Transform anchor = new GameObject("GateCameraAnchor").transform;
			anchor.position = cameraFollowSystem.PositionOfAttack;
			return anchor;
		}
	}
}
