using System.Linq;
using Codebase.Simulation.Forecast;
using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterStageView : View
	{
		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private GameObject checkMark;

		[SerializeField]
		private TMP_Text passedStageNumber;

		[SerializeField]
		private TMP_Text pendingStageNumber;

		[SerializeField]
		private GameObject dividerPassed;

		[SerializeField]
		private GameObject divider;

		[SerializeField]
		private GameObjectGroup passedState;

		[SerializeField]
		private GameObjectGroup pendingState;

		[SerializeField]
		private Button viewAllButton;

		[SerializeField]
		private Button hideButton;

		[SerializeField]
		private CanvasGroup rewardsCanvasGroup;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		[Inject]
		private ItemsProvider itemsProvider;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private bool forceShowAsPassed;

		private bool isLast;

		private int? boundRewardsStageIndex;

		private Action onExpandRequested;

		private Action onCollapseRequested;

		private bool isPassed;

		private bool hideCheck;

		private int StageIndex { get; set; }

		private EventModel CurrentEvent => eventsRepository.ScheduledEvent ?? eventsRepository.CurrentEvent ?? eventsRepository.CompletedEvent;

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x167074c.
			base.Awake();
			viewAllButton.onClick.AddListener(() => onExpandRequested?.Invoke());
		}

		protected override void Start()
		{
			// Original APK ARM64 ELF VA 0x16707e8.
			SetupRewardsContainer();
		}

		public void Setup(int stageIndex, bool isLast, Action onExpandRequested = null, Action onCollapseRequested = null, bool forceShowAsPassed = false, bool hideCheck = false)
		{
			// Original APK ARM64 ELF VA 0x166999c.
			StageIndex = stageIndex;
			this.isLast = isLast;
			this.hideCheck = hideCheck;
			this.forceShowAsPassed = forceShowAsPassed;
			this.onExpandRequested = onExpandRequested;
			this.onCollapseRequested = onCollapseRequested;
			if (onCollapseRequested != null) checkMark.SetActive(false);
			if (boundRewardsStageIndex != stageIndex)
			{
			    boundRewardsStageIndex = stageIndex;
			    rewardsViewContainer?.BindTo(Lifetime, GetStageRewardsWithoutEventReward);
			}
			Initialize();
		}

		public void UpdateExpandState(bool isCurrentRow, bool isExpanded)
		{
			// Original APK ARM64 ELF VA 0x1667d70.
			hideButton.gameObject.SetActive(false);
			viewAllButton.gameObject.SetActive(isCurrentRow && !isExpanded);
			checkMark.SetActive(!isCurrentRow && isPassed);
		}

		private void SetupDivider(bool isLast, bool isPassed)
		{
			// Original APK ARM64 ELF VA 0x1670a50.
			divider.SetActive(!isLast && !isPassed);
			dividerPassed.SetActive(isPassed && !isLast);
		}

		private void SetupRewardsContainer()
		{
			// Original APK ARM64 ELF VA 0x16707ec.
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContent, rewardViewPrefab)
			    .BindTo(Lifetime, GetStageRewardsWithoutEventReward).WithEqualityCheck(AreRewardsEqual)
			    .Setup((view, reward) =>
			    {
			        view.Setup(reward, RewardViewStyle.Simplified);
			        view.SimplifiedRewardGlow.SetActive(reward is StatReward stat && stat.CustomModifier && (int)stat.ModifierType == 7);
			        view.SetInfoBtnVisible(false);
			    });
		}

		private static bool AreRewardsEqual(RewardBase a, RewardBase b)
		{
			// Original APK ARM64 ELF VA 0x1670aa4.
			if (ReferenceEquals(a, b)) return true;
			if (a == null || b == null || a.GetType() != b.GetType()) return false;
			if (a is StatReward sa && b is StatReward sb)
			    return sa.Type == sb.Type && sa.Value.Equals(sb.Value) && sa.CustomModifier == sb.CustomModifier &&
			        sa.Name == sb.Name && sa.Source == sb.Source && sa.ModifierType == sb.ModifierType;
			if (a is ChestReward ca && b is ChestReward cb) return ca.Key == cb.Key && ca.Amount == cb.Amount;
			if (a is ItemReward ia && b is ItemReward ib)
			    return ia.IsRandom == ib.IsRandom && ia.SlotType == ib.SlotType && ia.Rarity == ib.Rarity &&
			        ia.Level == ib.Level && ia.ForceShowAsRandom == ib.ForceShowAsRandom && ia.Def == ib.Def &&
			        ia.BakedData?.ItemKey == ib.BakedData?.ItemKey;
			if (a is EventCurrencyReward ea && b is EventCurrencyReward eb) return ea.Key == eb.Key && ea.Amount.Equals(eb.Amount);
			return false;
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x1670e4c.
			var model = CurrentEvent;
			if (eventsRepository == null || model == null) return;
			isPassed = forceShowAsPassed || StageIndex < model.Progress;
			checkMark.SetActive(isPassed && !hideCheck);
			passedState.SetActive(isPassed);
			pendingState.SetActive(!isPassed);
			passedStageNumber.text = pendingStageNumber.text = (StageIndex + 1).ToString();
			rewardsCanvasGroup.alpha = isPassed ? 0.3f : 1f;
			SetupDivider(isLast, StageIndex + 1 < model.Progress);
		}

		private List<RewardBase> GetStageRewardsWithoutEventReward()
		{
			// Original APK ARM64 ELF VA 0x167100c.
			var model = CurrentEvent;
			var rewards = GetStageRewards();
			if (model == null || rewards == null) return new List<RewardBase>();
			bool random = !forceShowAsPassed && model.Progress <= StageIndex;
			foreach (ItemReward item in rewards.OfType<ItemReward>()) item.ForceShowAsRandom = random;
			return rewards.Where(reward => !(reward is EventReward)).ToList();
		}

		private EventReward GetEventReward()
		{
			// Original APK ARM64 ELF VA 0x1671590.
			return GetStageRewards().OfType<EventReward>().FirstOrDefault();
		}

		private List<RewardBase> GetStageRewards()
		{
			// Original APK ARM64 ELF VA 0x1671378.
			var stages = eventsRepository.EventPlan?.CrystalStages;
			return stages != null && StageIndex >= 0 && StageIndex < stages.Count && stages[StageIndex].Rewards != null
			    ? ForecastCrystalRewardEntryHelper.ToRewards(stages[StageIndex].Rewards, itemsProvider) : new List<RewardBase>();
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x1671608.
			base.Cleanup();
			rewardsViewContainer?.Clear();
		}
	}
}
