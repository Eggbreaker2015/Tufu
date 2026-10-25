using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Player.Models;
using Codebase.UI;
using Codebase.UI.RateUs;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using System;
using DG.Tweening;

namespace Codebase.Gates.Flows
{
	public class CollectPortalGemFlowRunner : IFlowRunner<CollectPortalGemFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__12 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public CollectPortalGemFlowRunner _003C_003E4__this;
			public CollectPortalGemFlow flow;
			private bool _003ChadGateOpening_003E5__2;
			private GameObject _003Cgem_003E5__3;
			private GameObject _003CcameraAnchor_003E5__4;
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
		private RateUsSystem rateUsSystem;
		[Inject]
		private UiModel uiModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		private const float SpiralHeight = 15f;
		private const float SpiralRadius = 2f;
		private const int SpiralPoints = 24;
		private const float SpiralRotations = 2f;
		private const float RiseUpDuration = 2f;
		private const float FlightSpeed = 70f;
		public async UniTask Execute(CollectPortalGemFlow flow)
		{
			try
			{
				playerModel.CanMove = false;
				await UniTask.WaitForEndOfFrame();
				await UniTask.WaitWhile(() => uiModel.IsItemSlotUnlockShowing);
				bool hadGateOpening = uiModel.IsGateOpeningShowing;
				await UniTask.WaitWhile(() => uiModel.IsGateOpeningShowing);
				if (flow.WaitForItemReward && !hadGateOpening)
					await UniTask.Delay(TimeSpan.FromSeconds(2));
				uiModel.IsHudVisible = false;
				await UniTask.Delay(TimeSpan.FromSeconds(1.5));
				GameObject gem = portal.GetStone(flow.GemIndex - 1);
				gem.SetActive(true);
				Vector3 start = playerModel.Unit.transform.position + playerModel.Unit.transform.forward * 2f;
				gem.transform.position = start;
				portal.PlayExplosion(gem.transform.position + Vector3.up * 3f);
				GameObject cameraAnchor = new GameObject("GemCameraAnchor");
				cameraAnchor.transform.position = start + Vector3.up * 15f;
				cameraFollowSystem.Follow(cameraAnchor.transform, playerModel.Unit.transform.position.y * 2f, 0f, false, 1f);
				gem.transform.DOPath(GenerateSpiralPath(start, SpiralHeight, SpiralRadius, SpiralPoints, SpiralRotations), RiseUpDuration, PathType.CatmullRom).SetEase(Ease.OutQuad);
				await UniTask.Delay(TimeSpan.FromSeconds(2));
				cameraFollowSystem.Follow(gem.transform, playerModel.Unit.transform.position.y * 2f);
				UnityEngine.Object.Destroy(cameraAnchor);
				Vector3 target = portal.GetStonePosition(flow.GemIndex - 1);
				float duration = Vector3.Distance(gem.transform.position, target) / FlightSpeed;
				gem.transform.DOMove(target, duration).SetEase(Ease.InOutSine);
				await UniTask.Delay(TimeSpan.FromSeconds(duration));
				portal.PlayExplosion(gem.transform.position);
				portal.DisableTrail();
				audioPlayer.PlaySoundOnce("crystal_equip");
				await UniTask.Delay(TimeSpan.FromSeconds(1));
				if (playerModel.PortalGemsCollected >= portal.GemsTotal && !playerModel.IsUnlockAnimationShown)
					await App.InitiateFlow(new PortalUnlockFlow());
				playerModel.CanMove = true;
				cameraFollowSystem.Unfollow();
				await UniTask.Delay(TimeSpan.FromSeconds(0.5));
				rateUsSystem.RequestRateUs(0f);
				uiModel.IsHudVisible = true;
			}
			finally
			{
				uiModel.IsPortalGemCollectShowing = false;
			}
		}

		private static Vector3[] GenerateSpiralPath(Vector3 start, float height, float radius, int points, float rotations)
		{
			Vector3[] path = new Vector3[points];
			for (int i = 0; i < points; i++)
			{
				float t = (float)i / (points - 1);
				float angle = t * rotations * Mathf.PI * 2f;
				float r = (1f - t * 0.5f) * radius;
				path[i] = start + new Vector3(Mathf.Cos(angle) * r, t * height, Mathf.Sin(angle) * r);
			}

			return path;
		}
	}
}
