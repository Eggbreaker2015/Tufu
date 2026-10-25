using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Shop;
using Codebase.Simulation.Forecast;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventProgressView : PopupView
	{
		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[Header("Currencies")]
		[SerializeField]
		private Transform currenciesContainer;

		[SerializeField]
		private EventCurrencyView currencyViewPrefab;

		[Header("Boosters")]
		[SerializeField]
		private Transform boostersContainer;

		[SerializeField]
		private EventBoosterView boosterViewPrefab;

		[Header("Stages Section")]
		[SerializeField]
		private Transform stagesContent;

		[SerializeField]
		private RectTransform stagesWindow;

		[SerializeField]
		private RosterStageView stageViewPrefab;

		[SerializeField]
		private RosterStagesExpandPanel stagesExpandPanel;

		[SerializeField]
		private Button collapseBtnBg;

		[SerializeField]
		private Button collapseBtn;

		[Header("Milestone Chests Section")]
		[SerializeField]
		private RosterMilestoneChestsPanel milestoneChestsPanel;

		[Header("ProgressPanel")]
		[SerializeField]
		private RosterLeagueProgressPanel leagueProgressPanel;

		[SerializeField]
		private TextMeshProUGUI redCrystalsAmountTxt;

		[SerializeField]
		private TextMeshProUGUI blueCrystalsAmountTxt;

		[Header("Header")]
		[SerializeField]
		private Image resistIcon;

		[SerializeField]
		private Image weaponIcon;

		[SerializeField]
		private TMP_Text timerText;

		[SerializeField]
		private TMP_Text mobCountText;

		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text crystalDamageStatTxt;

		[SerializeField]
		private TMP_Text crystalDefenceStatTxt;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventsStyle;

		[Inject]
		private IconStyleConfig iconStyle;

		[Inject]
		private ChestQueueModel chestQueue;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private PlayerModel playerModel;

		private ViewContainer<EventCurrencyView, string> currenciesViewContainer;

		private ViewContainer<EventBoosterView, EventBoosterModel> boostersViewContainer;

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private int selectedMilestoneIndex;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		protected override void Start()
		{
		}

		private void RequestCollapse()
		{
		}

		public override void Initialize()
		{
		}

		private string BuildTimerText()
		{
			return null;
		}

		protected override void Render()
		{
		}

		private void UpdateStagesExpandPanel()
		{
		}

		private int GetCurrentStageIndex()
		{
			return 0;
		}

		private void HandleExpandStage()
		{
		}

		private void HandleCollapseStage()
		{
		}

		private void HandleMilestoneChestClicked(int milestoneIndex)
		{
		}

		private void CloseMilestoneChestTooltip()
		{
		}

		private void GrantMilestoneRewards(List<RewardBase> rewards)
		{
		}

		private void BakeGrantedItemCopies(RewardBase reward)
		{
		}

		private void UpdateHeaders(EventModel currentEvent)
		{
		}

		private void UpdateEventTitleText(EventModel currentEvent)
		{
		}

		private void UpdateLeagueProgressText(EventModel currentEvent)
		{
		}

		private List<int> GetStageIndices()
		{
			return null;
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			return null;
		}

		private List<RewardBase> GetMilestoneRewards(int milestoneIndex)
		{
			return null;
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			return null;
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

		public override void Cleanup()
		{
		}
	}
}
