using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Rewards;
using Codebase.UI.IslandNotification;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventStartView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003COnDifficultySelected_003Ed__43 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncVoidMethodBuilder _003C_003Et__builder;

			public EventStartView _003C_003E4__this;

			public EventDifficulty difficulty;

			private EventModel _003CcurrentEvent_003E5__2;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

			private UniTask.Awaiter _003C_003Eu__2;

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
		private struct _003CStartEvent_003Ed__48 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncVoidMethodBuilder _003C_003Et__builder;

			public EventStartView _003C_003E4__this;

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

		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button startEventButton;

		[SerializeField]
		private Image startEventButtonImage;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text durationText;

		[Header("Difficulty Selection")]
		[SerializeField]
		private GameObject difficultySection;

		[SerializeField]
		private GameObject difficultyPlaceholder;

		[SerializeField]
		private Transform difficultyButtonsContainer;

		[SerializeField]
		private DifficultyButton difficultyButtonPrefab;

		[SerializeField]
		private TMP_Text difficultyText;

		[Header("Start Event Section")]
		[SerializeField]
		private GameObject startEventSection;

		[Header("Rewards Section")]
		[SerializeField]
		private GameObject rewardsContainer;

		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[Header("Progress Bar")]
		[SerializeField]
		private EventProgressBarView progressBarView;

		[SerializeField]
		private TMP_Text progressText;

		[Header("Event Info")]
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
		private IAudioPlayer audioPlayer;

		private ViewContainer<DifficultyButton, EventDifficulty> difficultyButtonsViewContainer;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private EventDifficulty? lastRenderedDifficulty;

		private bool isStartingEvent;

		private ComputedAtom<EventModel> __CurrentEvent;

		private ComputedAtom<EventDifficulty?> __SelectedDifficulty;

		private ComputedAtom<EventDifficultySettings> __SelectedDifficultyDef;

		private ComputedAtom<bool> __HasDifficultySelected;

		private EventModel CurrentEvent => null;

		private EventDifficulty? SelectedDifficulty => null;

		[CanBeNull]
		private EventDifficultySettings SelectedDifficultyDef => null;

		private bool HasDifficultySelected => false;

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

		private void UpdateDurationText(EventModel currentEvent)
		{
		}

		private void UpdateStartButtonStyle(EventDifficulty difficulty)
		{
		}

		[AsyncStateMachine(typeof(_003COnDifficultySelected_003Ed__43))]
		private void OnDifficultySelected(EventDifficulty difficulty)
		{
		}

		private void RebuildProgressBar()
		{
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

		[AsyncStateMachine(typeof(_003CStartEvent_003Ed__48))]
		private void StartEvent()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
