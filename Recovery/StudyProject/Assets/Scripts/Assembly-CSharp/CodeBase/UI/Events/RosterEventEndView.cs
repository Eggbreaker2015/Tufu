using System.Collections.Generic;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
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

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ComputedAtom<EventModel> __CompletedEvent;

		private EventModel CompletedEvent => null;

		protected override void Awake()
		{
		}

		public void StartScroll()
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

		private void UpdateEventTitleText(EventModel currentEvent)
		{
		}

		protected override void Render()
		{
		}

		private List<int> GetStageIndices()
		{
			return null;
		}

		private void ClaimRewards()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
