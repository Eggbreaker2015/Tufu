using System.Linq;
using UniMob;
using Cysharp.Threading.Tasks;
using Codebase.Utilities;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using Codebase.Unit;
using System.Collections.Generic;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Core.UserData;
using Codebase.Events;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Simulation.Forecast;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventEndView : PopupView
	{
		[SerializeField]
		private AnimationSequencerController controller;

		[SerializeField]
		private RosterMilestoneChestsPanel milestoneChestsPanel;

		[SerializeField]
		private TextMeshProUGUI eventTitleText;

		[Header("Buttons")]
		[SerializeField]
		private Button claimButton;

		[Header("Stages Section")]
		[SerializeField]
		private Transform stagesContent;

		[SerializeField]
		private RosterStageView stageViewPrefab;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyle;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		[Inject]
		private UINavigator navigator;

        [Inject] private ItemsProvider itemsProvider;

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ComputedAtom<EventModel> __CompletedEvent;

		private EventModel CompletedEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x166388c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CompletedEvent == null) __CompletedEvent = CodeGenAtom.CreatePooled(this, null, () => CompletedEvent, false);
                return __CompletedEvent.DirectEvaluate() ? eventsRepository.CompletedEvent : __CompletedEvent.Value;
            }
        }

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x16639e0.
			base.Awake();
			controller.OnFinishedEvent.AddListener(StartScroll);
			claimButton.onClick.AddListener(ClaimRewards);
		}

		public void StartScroll()
		{
			// Original APK ARM64 ELF VA 0x1663ad4.
			milestoneChestsPanel.ScrollToLastMilestoneInstantForFrames(5).Forget();
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x1663afc.
			base.Initialize();
			UpdateEventTitleText(CompletedEvent);
			controller.PlayForward();
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			// Original APK ARM64 ELF VA 0x1663d90.
			return GetPlanMilestones() ?? new List<ForecastCrystalMilestonePlan>();
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			// Original APK ARM64 ELF VA 0x1663e0c.
			return eventsRepository.EventPlan?.CrystalMilestones;
		}

		protected override void Start()
		{
			// Original APK ARM64 ELF VA 0x1663f68.
			base.Start();
			App.ExecuteCommand(new BakeRewardsCommand { BakeRosterEvent = true });
			milestoneChestsPanel.Initialize(Lifetime, GetMilestones, null, false);
			int count = GetStageIndices().Count;
			stagesViewContainer = new ViewContainer<RosterStageView, int>(stagesContent, stageViewPrefab)
			    .BindTo(Lifetime, GetStageIndices).WithEqualityCheck((a, b) => a == b).Setup((view, data, index) =>
			    {
			        view.Setup(data, index == count - 1);
			        view.UpdateExpandState(false, true);
			    });
			if (CompletedEvent != null)
			{
			    milestoneChestsPanel.UpdateState(CompletedEvent, GetMilestones());
			    milestoneChestsPanel.ScrollToLastMilestoneInstantForFrames(5).Forget();
			}
		}

		private void UpdateEventTitleText(EventModel currentEvent)
		{
			// Original APK ARM64 ELF VA 0x1663b44.
			var plan = eventsRepository.EventPlan;
			if (currentEvent == null || !plan.HasValue) return;
			var league = currentEvent.Def.CrystalRoster.Leagues[plan.Value.LeagueIndex];
			eventTitleText.text = "<color=" + league.TitleColor + ">" + LocalizationStrings.Get("EVENT_LEAGUE_TITLE_" + league.Name) + "</color>";
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x166443c.
			if (CompletedEvent == null) return;
			milestoneChestsPanel.UpdateState(CompletedEvent, GetMilestones());
			UpdateEventTitleText(CompletedEvent);
		}

		private List<int> GetStageIndices()
		{
			// Original APK ARM64 ELF VA 0x166436c.
			return CompletedEvent?.Def?.CrystalRoster?.Stages != null ? Enumerable.Range(0, CompletedEvent.Def.CrystalRoster.Stages.Count).ToList() : new List<int>();
		}

		private void ClaimRewards()
		{
			// Original APK ARM64 ELF VA 0x16644a4.
			var model = CompletedEvent;
			if (model == null) return;
			// Learning adaptation for the explicitly excluded mailbox:
			// Native CompleteEvent mailed earned unclaimed milestones. Deliver the same plan
			// entries locally on Claim, using the original threshold and persisted claimed count.
			var milestones = GetPlanMilestones();
			if (model.IsCrystalRosterEvent && milestones != null)
			{
			    double collected = model.GetCollectedCurrency("red_crystal") + model.GetCollectedCurrency("blue_crystal");
			    while (model.RosterMilestoneClaimedCount < milestones.Count)
			    {
			        var milestone = milestones[model.RosterMilestoneClaimedCount];
			        if (collected < milestone.RequiredCrystals) break;
			        var rewards = ForecastCrystalRewardEntryHelper.ToRewards(milestone.Rewards, itemsProvider);
			        App.ExecuteCommand(new BakeRewardsCommand { Rewards = rewards });
			        foreach (var reward in rewards)
			            App.ExecuteCommand(new GrantRewardCommand { Reward = reward, SourcePath = "event.prize", SourceOverride = ModifierSource.CrystalEvent });
			        model.RosterMilestoneClaimedCount++;
			    }
			}
			eventAnalyticsModel?.ReportCompleted(model);
			eventsRepository.ClaimEvent(model.Key);
			// Local learning adaptation: commit actual rewards together with the claimed/reset event state.
			GameContext.Get<GameDataService>().ForceSave();
			Close();
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x1664518.
			base.Cleanup();
			milestoneChestsPanel.Cleanup();
		}
	}
}
