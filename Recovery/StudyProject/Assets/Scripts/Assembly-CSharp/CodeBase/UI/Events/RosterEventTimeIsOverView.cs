using System;
using System.Linq;
using UniMob;
using Cysharp.Threading.Tasks;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using Codebase.Utilities;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using Codebase.Events.Systems;
using Codebase.Player.Inventory;
using Codebase.Simulation.Forecast;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventTimeIsOverView : PopupView
	{
		[SerializeField]
		private RosterLeagueProgressPanel leagueProgressPanel;

		[SerializeField]
		private RosterMilestoneChestsPanel milestoneChestsPanel;

		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text progressText;

		[Header("Boosters")]
		[SerializeField]
		private Transform boostersContainer;

		[SerializeField]
		private EventBoosterView boosterViewPrefab;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private EventTimerSystem eventTimerSystem;

		private readonly List<EventBoosterView> spawnedBoosterViews = new List<EventBoosterView>();

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x166cb30.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? eventsRepository.CurrentEvent : __CurrentEvent.Value;
            }
        }

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x166cc6c.
			base.Awake();
			closeButton.onClick.AddListener(Close);
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x166cd08.
			base.Initialize();
			App.ExecuteCommand(new BakeRewardsCommand { BakeRosterEvent = true });
			SetupBoosters();
			milestoneChestsPanel.ScrollToLastMilestoneInstantForFrames(2).Forget();
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			// Original APK ARM64 ELF VA 0x166d21c.
			return GetPlanMilestones() ?? new List<ForecastCrystalMilestonePlan>();
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			// Original APK ARM64 ELF VA 0x166d298.
			return eventsRepository.EventPlan?.CrystalMilestones;
		}

		protected override void Start()
		{
			// Original APK ARM64 ELF VA 0x166d3f4.
			base.Start();
			milestoneChestsPanel.Initialize(Lifetime, GetMilestones, null, false);
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x166d4ac.
			var model = CurrentEvent;
			var plan = eventsRepository.EventPlan;
			if (model == null || !plan.HasValue) return;
			int index = plan.Value.LeagueIndex;
			var leagues = model.Def.CrystalRoster.Leagues;
			var league = leagues[index];
			var next = leagues[Math.Min(index + 1, leagues.Count - 1)];
			eventTitleText.text = "<color=" + league.TitleColor + ">" + LocalizationStrings.Get("EVENT_LEAGUE_TITLE_" + league.Name) + "</color>";
			int bossesLeft = league.BossesToAdvance - model.Progress;
			if (index < model.RosterLeagueIndex || model.MaxLeagueReached)
			    progressText.text = LocalizationStrings.Get("EVENT_BOSSES_BEATEN").Replace("{current}", model.Progress.ToString()).Replace("{max}", model.ProgressMax.ToString());
			else
			    progressText.text = LocalizationStrings.Get("EVENT_BOSSES_LEFT_TO_LEAGUE")
			        .Replace("{color}", next.TitleColor ?? "")
			        .Replace("{league}", LocalizationStrings.Get("EVENT_LEAGUE_NAME_" + next.Name))
			        .Replace("{value}", bossesLeft > 0 ? bossesLeft.ToString() : string.Format("{0}/{1}", model.ProgressMax, model.ProgressMax));
			milestoneChestsPanel.UpdateState(model, GetMilestones());
			leagueProgressPanel.UpdateProgress(model, plan);
		}

		private void SetupBoosters()
		{
			// Original APK ARM64 ELF VA 0x166cdc4.
			ClearBoosters();
			if (CurrentEvent == null) return;
			foreach (EventBoosterModel booster in CurrentEvent.Boosters.Where(HasExtraTimeReward))
			{
			    var view = Instantiate(boosterViewPrefab, boostersContainer);
			    Injector.InjectInto(view);
			    view.Setup(booster);
			    spawnedBoosterViews.Add(view);
			}
		}

		private bool HasExtraTimeReward(EventBoosterModel boosterModel)
		{
			// Original APK ARM64 ELF VA 0x166df9c.
			return boosterModel.RemainingPurchases > 0 && (boosterModel.Def.Rewards?.OfType<EventReward>().Any() ?? false);
		}

		private void ClearBoosters()
		{
			// Original APK ARM64 ELF VA 0x166dcfc.
			foreach (var view in spawnedBoosterViews) if (view != null) Destroy(view.gameObject);
			spawnedBoosterViews.Clear();
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x166e118.
			base.Cleanup();
			milestoneChestsPanel.Cleanup();
			ClearBoosters();
		}
	}
}
