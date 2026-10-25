using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.UI.IslandNotification;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventStartView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildPreviewEventPlan_003Ed__38 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public RosterEventStartView _003C_003E4__this;

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
		private struct _003CStartEvent_003Ed__39 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncVoidMethodBuilder _003C_003Et__builder;

			public RosterEventStartView _003C_003E4__this;

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

		[SerializeField]
		private RosterLeagueProgressPanel leagueProgressPanel;

		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button startEventButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text durationText;

		[SerializeField]
		private TMP_Text bossesCounterTxt;

		[Header("Rewards Section")]
		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private IslandNotificationMapInfoView weaknessInfo;

		[SerializeField]
		private IslandNotificationMapInfoView attackInfo;

		[SerializeField]
		private Color weaknessColor;

		[SerializeField]
		private Color attackColor;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private IAudioPlayer audioPlayer;

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private bool isStartingEvent;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		protected override void Start()
		{
		}

		public override void Initialize()
		{
		}

		protected override void Render()
		{
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			return null;
		}

		private List<RewardBase> GetMilestoneRewards(int milestoneIndex)
		{
			return null;
		}

		private List<RewardBase> GetSummaryRewards()
		{
			return null;
		}

		private ItemConfig GetItemDef(ItemReward itemReward)
		{
			return null;
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			return null;
		}

		private List<int> GetStageIndices()
		{
			return null;
		}

		private void SetupEventInfo()
		{
		}

		private void HandleClose()
		{
		}

		private void OpenTutorial()
		{
		}

		private void OpenRewards()
		{
		}

		[AsyncStateMachine(typeof(_003CBuildPreviewEventPlan_003Ed__38))]
		private UniTaskVoid BuildPreviewEventPlan()
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CStartEvent_003Ed__39))]
		private void StartEvent()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
