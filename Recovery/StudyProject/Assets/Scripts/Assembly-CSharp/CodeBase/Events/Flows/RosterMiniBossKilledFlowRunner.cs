using System;
using System.Linq;
using Codebase.Simulation.Forecast;
using Codebase.Rewards.Commands;
using Codebase.Unit;
using DG.Tweening;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Events.Components;
using Codebase.Events.Models;
using Codebase.Events.Systems;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Shop;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.Events.Flows
{
	public class RosterMiniBossKilledFlowRunner : IFlowRunner<RosterMiniBossKilledFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAnimateCameraToChest_003Ed__13 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public Transform chestRoot;

			public Transform anchor;

			private Vector3 _003CtargetPosition_003E5__2;

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
		private struct _003CExecute_003Ed__11 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public RosterMiniBossKilledFlowRunner _003C_003E4__this;

			public RosterMiniBossKilledFlow flow;

			private EventModel _003CcurrentEvent_003E5__2;

			private CrystalEventRosterConfig _003CrosterConfig_003E5__3;

			private bool _003ChasChest_003E5__4;

			private EventBossChestComponent _003Cchest_003E5__5;

			private Transform _003CcameraAnchor_003E5__6;

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
		private struct _003CPlayChestAnimation_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventBossChestComponent chest;

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

		private const float CameraFlightDuration = 0.5f;

		private const float CameraFollowDistance = -5f;

		private const float WaitAfterChestOpen = 0.5f;

		private const float WaitBeforeNextSpawn = 5f;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EventBossGateSystem eventBossGateSystem;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		[Inject]
		private ChestQueueModel chestQueue;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private UINavigator navigator;

		public async UniTask Execute(RosterMiniBossKilledFlow flow)
		{
			// Original APK ARM64 ELF VA 0x14f9fdc.
			EventModel model = eventsRepository.CurrentEvent ?? eventsRepository.CompletedEvent;
			if (model == null || model.Def.CrystalRoster == null) return;
			eventBossGateSystem.SetMiniBossFlowRunning(true);
			EventBossChestComponent chest = default;
			bool hasChest = eventBossGateSystem.TryGetFirstAvailableChest(eventBossGateSystem.GetChestDifficultyForStage(model, flow.ProgressIndex), out chest);
			Transform anchor = null;
			try
			{
			    if (hasChest)
			    {
			        anchor = CreateCameraAnchor();
			        cameraFollowSystem.Follow(anchor, CameraFollowDistance);
			        await AnimateCameraToChest(anchor, chest.Root.transform);
			        anchor.SetParent(chest.Root.transform, true);
			        await PlayChestAnimation(chest);
			    }
			    var rewards = GetStageRewards(model.Def.CrystalRoster, flow.ProgressIndex);
			    if (rewards != null && rewards.Count > 0)
			    {
			        App.ExecuteCommand(new BakeRewardsCommand { Rewards = rewards });
			        EnqueueRewards(rewards);
			    }
			    if (hasChest)
			    {
			        cameraFollowSystem.Unfollow();
			        UnityEngine.Object.Destroy(anchor.gameObject);
			        ResetChestAnimation(chest);
			    }
			    await chestQueue.WaitUntilQueueEmpty();
			    if (flow.DidAdvanceLeague && model.IsActive)
			        navigator.Open<PopupView>("Assets/Prefabs/UI/Event/Roster/RosterEventEndLeagueNotificationView.prefab");
			    if (model.Progress < model.ProgressMax)
			    {
			        await UniTask.Delay(TimeSpan.FromSeconds(WaitBeforeNextSpawn));
			        await App.InitiateFlow(new RosterSpawnNextMiniBossFlow { EventKey = model.Key });
			    }
			}
			finally
			{
			    eventBossGateSystem.SetMiniBossFlowRunning(false);
			}
		}

		private Transform CreateCameraAnchor()
		{
			// Original APK ARM64 ELF VA 0x14f8c5c.
			var anchor = new GameObject("RosterChestCameraAnchor").transform;
			anchor.position = playerModel.Unit.transform.position;
			return anchor;
		}

		private async UniTask AnimateCameraToChest(Transform anchor, Transform chestRoot)
		{
			// Original APK ARM64 ELF VA 0x14f9b70.
			Vector3 target = chestRoot.position;
			anchor.DOMove(target, CameraFlightDuration).SetEase((Ease)6);
			await UniTask.Delay(TimeSpan.FromSeconds(CameraFlightDuration));
			anchor.position = target;
		}

		private async UniTask PlayChestAnimation(EventBossChestComponent chest)
		{
			// Original APK ARM64 ELF VA 0x14fb0dc.
			if (chest.Animation != null) await chest.Animation.PlayAsync();
			await UniTask.Delay(TimeSpan.FromSeconds(WaitAfterChestOpen));
		}

		private void ResetChestAnimation(EventBossChestComponent chest)
		{
			// Original APK ARM64 ELF VA 0x14f8e80.
			if (chest.Animation == null) return;
			chest.Animation.ClearPlayingSequence();
			chest.Animation.ResetToInitialState();
		}

		private List<RewardBase> GetStageRewards(CrystalEventRosterConfig rosterConfig, int stageIndex)
		{
			// Original APK ARM64 ELF VA 0x14f8f1c.
			var stages = eventsRepository.EventPlan?.CrystalStages;
			return stages != null && stageIndex >= 0 && stageIndex < stages.Count && stages[stageIndex].Rewards != null
			    ? ForecastCrystalRewardEntryHelper.ToRewards(stages[stageIndex].Rewards, itemsProvider) : new List<RewardBase>();
		}

		private void EnqueueRewards(List<RewardBase> rewards)
		{
			// Original APK ARM64 ELF VA 0x14f9154.
			var displayed = rewards.Where(reward => reward is StatReward || reward is ItemReward).ToList();
			var others = rewards.Where(reward => !(reward is StatReward) && !(reward is ItemReward)).ToList();
			foreach (RewardBase reward in others)
			    App.ExecuteCommand(new GrantRewardCommand { Reward = reward, SourcePath = "event.prize", SourceOverride = (ModifierSource)12 });
			foreach (RewardBase reward in displayed)
			{
			    App.ExecuteCommand(new GrantRewardCommand { Reward = reward, SourcePath = "event.prize", SourceOverride = (ModifierSource)12 });
			    BakeGrantedItemCopies(reward);
			}
			if (displayed.Count > 0)
			{
			    chestQueue.EnqueueRewards(displayed, "general");
			    App.RequestAppUpdateFlow();
			}
		}

		private void BakeGrantedItemCopies(RewardBase reward)
		{
			// Original APK ARM64 ELF VA 0x14f97a0.
			if (!(reward is ItemReward item)) return;
			string key = item.BakedData?.ItemKey ?? item.Def?.Key;
			item.BakedTotalCopies = playerModel.Items.FirstOrDefault(model => model.Key == key)?.Copies;
		}
	}
}
