using System;
using System.Linq;
using UniMob;
using Cysharp.Threading.Tasks;
using Codebase.Services.Time;
using Codebase.Utilities;
using Codebase.Rewards.Commands;
using Codebase.Unit;
using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Core.UserData;
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

		private int selectedMilestoneIndex = -1;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x1665844.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? eventsRepository.CurrentEvent : __CurrentEvent.Value;
            }
        }

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x1665970.
			base.Awake();
			closeButton.onClick.AddListener(HandleClose);
			infoButton.onClick.AddListener(OpenTutorial);
			collapseBtnBg.onClick.AddListener(RequestCollapse);
			collapseBtn.onClick.AddListener(RequestCollapse);
		}

		protected override void Start()
		{
			// Original APK ARM64 ELF VA 0x1665af0.
			base.Start();
			App.ExecuteCommand(new BakeRewardsCommand { BakeRosterEvent = true });
			currenciesViewContainer = new ViewContainer<EventCurrencyView, string>(currenciesContainer, currencyViewPrefab)
			    .BindTo(Lifetime, () => CurrentEvent?.Def.Currencies).Setup((view, key) => view.Setup(key));
			boostersViewContainer = new ViewContainer<EventBoosterView, EventBoosterModel>(boostersContainer, boosterViewPrefab)
			    .BindTo(Lifetime, () => CurrentEvent?.Boosters).Setup((view, model) => view.Setup(model));
			int count = GetStageIndices().Count;
			Injector.InjectInto(stagesExpandPanel.collapsedPreviewView);
			stagesViewContainer = new ViewContainer<RosterStageView, int>(stagesContent, stageViewPrefab)
			    .BindTo(Lifetime, GetStageIndices).WithEqualityCheck((a, b) => a == b).Setup((view, data, index) =>
			    {
			        Injector.InjectInto(view);
			        view.Setup(data, index == count - 1, null, HandleCollapseStage);
			    });
			stagesExpandPanel.Initialize(count);
			milestoneChestsPanel.Initialize(Lifetime, GetMilestones, HandleMilestoneChestClicked, true);
			milestoneChestsPanel.UpdateState(CurrentEvent, GetMilestones());
			UpdateStagesExpandPanel();
			Atom.Reaction(Lifetime, () => { Ticker.TickEveryFrame(); timerText.text = BuildTimerText(); });
		}

		private void RequestCollapse()
		{
			// Original APK ARM64 ELF VA 0x1667064.
			HandleCollapseStage();
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x16670a4.
			base.Initialize();
			milestoneChestsPanel.ScrollToLastMilestoneInstantForFrames(2).Forget();
		}

		private string BuildTimerText()
		{
			// Original APK ARM64 ELF VA 0x1667180.
			return string.Concat("<sprite name=", '"', eventsStyle.TimeSprite.name, '"', "> ", TimeLocalizer.GetFormatTime((float)eventsRepository.TimeUntilScheduledEventEnds.TotalSeconds, true));
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x1667280.
			var model = CurrentEvent;
			if (model == null) { Close(); return; }
			_ = model.Progress;
			_ = model.SpawnerTick;
			UpdateEventTitleText(model);
			resistIcon.sprite = iconStyle.ResistStyles.First(style => style.WeaponType == model.Data.ResistType.Value).Effective.Medium;
			weaponIcon.sprite = iconStyle.ResistStyles.First(style => style.WeaponType == model.Data.WeaponType.Value).Ineffective.High;
			UpdateHeaders(model);
			UpdateLeagueProgressText(model);
			milestoneChestsPanel.UpdateState(model, GetMilestones());
			UpdateStagesExpandPanel();
		}

		private void UpdateStagesExpandPanel()
		{
			// Original APK ARM64 ELF VA 0x1666e58.
			if (stagesViewContainer == null) return;
			int index = GetCurrentStageIndex();
			stagesExpandPanel.SetCurrentStage(index, CurrentEvent.Progress, HandleExpandStage);
			for (int row = 0; row < stagesViewContainer.Active.Count; row++)
			    stagesViewContainer.Active[row].UpdateExpandState(row == index, stagesExpandPanel.IsExpanded);
		}

		private int GetCurrentStageIndex()
		{
			// Original APK ARM64 ELF VA 0x1667c5c.
			return Math.Max(0, Math.Min(CurrentEvent?.Progress ?? 0, GetStageIndices().Count - 1));
		}

		private void HandleExpandStage()
		{
			// Original APK ARM64 ELF VA 0x1667dfc.
			stagesExpandPanel.SetExpanded(true, GetCurrentStageIndex());
			UpdateStagesExpandPanel();
		}

		private void HandleCollapseStage()
		{
			// Original APK ARM64 ELF VA 0x1667068.
			stagesExpandPanel.SetExpanded(false, GetCurrentStageIndex());
			UpdateStagesExpandPanel();
		}

		private void HandleMilestoneChestClicked(int milestoneIndex)
		{
			// Original APK ARM64 ELF VA 0x1668210.
			var model = CurrentEvent;
			var milestone = GetPlanMilestones()[milestoneIndex];
			var rewards = GetMilestoneRewards(milestoneIndex);
			double collected = model.GetCollectedCurrency("red_crystal") + model.GetCollectedCurrency("blue_crystal");
			if (model.RosterMilestoneClaimedCount != milestoneIndex || !(milestone.RequiredCrystals <= collected)) return;
			CloseMilestoneChestTooltip();
			GrantMilestoneRewards(rewards);
			model.RosterMilestoneClaimedCount++;
			// Local learning adaptation: persist the synchronous grant and claim marker together before Editor Stop.
			GameContext.Get<GameDataService>().ForceSave();
			milestoneChestsPanel.UpdateState(model, GetMilestones());
		}

		private void CloseMilestoneChestTooltip()
		{
			// Original APK ARM64 ELF VA 0x1668550.
			selectedMilestoneIndex = -1;
			milestoneChestsPanel.UpdateState(CurrentEvent, GetMilestones());
		}

		private void GrantMilestoneRewards(List<RewardBase> rewards)
		{
			// Original APK ARM64 ELF VA 0x1668598.
			App.ExecuteCommand(new BakeRewardsCommand { Rewards = rewards });
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
			// Original APK ARM64 ELF VA 0x1668c40.
			if (!(reward is ItemReward item)) return;
			string key = item.BakedData?.ItemKey ?? item.Def?.Key;
			item.BakedTotalCopies = playerModel.Items.FirstOrDefault(model => model.Key == key)?.Copies;
		}

		private void UpdateHeaders(EventModel currentEvent)
		{
			// Original APK ARM64 ELF VA 0x1667820.
			var mobs = eventsRepository.GetEventMobCounts(currentEvent);
			mobCountText.text = string.Format("{0}/{1}", mobs.Item1, mobs.Item2);
		}

		private void UpdateEventTitleText(EventModel currentEvent)
		{
			// Original APK ARM64 ELF VA 0x16675d4.
			var plan = eventsRepository.EventPlan;
			if (!plan.HasValue) return;
			var league = currentEvent.Def.CrystalRoster.Leagues[plan.Value.LeagueIndex];
			eventTitleText.text = "<color=" + league.TitleColor + ">" + LocalizationStrings.Get("EVENT_LEAGUE_TITLE_" + league.Name) + "</color>";
		}

		private void UpdateLeagueProgressText(EventModel currentEvent)
		{
			// Original APK ARM64 ELF VA 0x16678f8.
			var plan = eventsRepository.EventPlan;
			if (!plan.HasValue) return;
			leagueProgressPanel.UpdateProgress(currentEvent, plan);
			crystalDamageStatTxt.text = string.Format("<sprite name=atlas_57>{0}%", (int)(EventBalance.GetPlayerDamageMultiplier() * 100));
			crystalDefenceStatTxt.text = string.Format("<sprite name=atlas_58>{0}%", (int)((1 - EventBalance.GetEnemyDamageMultiplier()) * 100));
			redCrystalsAmountTxt.text = currentEvent.GetCurrency("red_crystal").Format();
			blueCrystalsAmountTxt.text = currentEvent.GetCurrency("blue_crystal").Format();
		}

		private List<int> GetStageIndices()
		{
			// Original APK ARM64 ELF VA 0x1666348.
			return CurrentEvent?.Def?.CrystalRoster?.Stages != null ? Enumerable.Range(0, CurrentEvent.Def.CrystalRoster.Stages.Count).ToList() : new List<int>();
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			// Original APK ARM64 ELF VA 0x16669d0.
			return GetPlanMilestones() ?? new List<ForecastCrystalMilestonePlan>();
		}

		private List<RewardBase> GetMilestoneRewards(int milestoneIndex)
		{
			// Original APK ARM64 ELF VA 0x16684d4.
			return ForecastCrystalRewardEntryHelper.ToRewards(GetPlanMilestones()[milestoneIndex].Rewards, itemsProvider);
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			// Original APK ARM64 ELF VA 0x1668378.
			return eventsRepository.EventPlan?.CrystalMilestones;
		}

		private void HandleClose()
		{
			// Original APK ARM64 ELF VA 0x1669220.
			audioPlayer.PlaySoundOnce("tap");
			Close();
		}

		private void OpenTutorial()
		{
			// Original APK ARM64 ELF VA 0x16692ec.
			audioPlayer.PlaySoundOnce("tap");
			navigator.Open<EventTutorialView>("Assets/Prefabs/UI/Event/Roster/RosterEventTutorial.prefab");
		}

		private void OpenRewards()
		{
			// Original APK ARM64 ELF VA 0x16693ec.
			audioPlayer.PlaySoundOnce("tap");
			navigator.Open<RosterEventRewardsView>("Assets/Prefabs/UI/Event/Roster/RosterRewardsPopup.prefab");
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x16694ec.
			base.Cleanup();
			stagesExpandPanel.Cleanup();
			milestoneChestsPanel.Cleanup();
		}
	}
}
