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

		[AsyncStateMachine(typeof(_003CExecute_003Ed__11))]
		public UniTask Execute(RosterMiniBossKilledFlow flow)
		{
			return default;
		}

		private Transform CreateCameraAnchor()
		{
			return null;
		}

		[AsyncStateMachine(typeof(_003CAnimateCameraToChest_003Ed__13))]
		private UniTask AnimateCameraToChest(Transform anchor, Transform chestRoot)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CPlayChestAnimation_003Ed__14))]
		private UniTask PlayChestAnimation(EventBossChestComponent chest)
		{
			return default;
		}

		private void ResetChestAnimation(EventBossChestComponent chest)
		{
		}

		private List<RewardBase> GetStageRewards(CrystalEventRosterConfig rosterConfig, int stageIndex)
		{
			return null;
		}

		private void EnqueueRewards(List<RewardBase> rewards)
		{
		}

		private void BakeGrantedItemCopies(RewardBase reward)
		{
		}
	}
}
