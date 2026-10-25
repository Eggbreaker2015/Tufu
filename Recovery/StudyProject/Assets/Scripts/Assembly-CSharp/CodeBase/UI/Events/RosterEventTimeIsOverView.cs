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

		private readonly List<EventBoosterView> spawnedBoosterViews;

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			return null;
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			return null;
		}

		protected override void Start()
		{
		}

		protected override void Render()
		{
		}

		private void SetupBoosters()
		{
		}

		private bool HasExtraTimeReward(EventBoosterModel boosterModel)
		{
			return false;
		}

		private void ClearBoosters()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
