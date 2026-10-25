using System.Linq;
using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventMilestoneView : View
	{
		[SerializeField]
		private Button chestButton;

		[SerializeField]
		private TMP_Text thresholdText;

		[SerializeField]
		private GameObjectGroup readyState;

		[SerializeField]
		private GameObjectGroup claimedState;

		[SerializeField]
		private RosterDivider prevDivider;

		[SerializeField]
		private RosterDivider finalDivider;

		[SerializeField]
		private TextMeshProUGUI progressTxt;

		[SerializeField]
		private TextMeshProUGUI specialItemRewardTxt;

		[SerializeField]
		private GameObject collectedMarker;

		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private GameObject readyEffect;

		[SerializeField]
		private Slider currentProgressSlider;

		[SerializeField]
		private GameObject currentCrystalsSection;

		[SerializeField]
		private TextMeshProUGUI currentCrystals;

		[SerializeField]
		private CanvasGroup targetedCanvasGroup;

		[SerializeField]
		private Image chestImage;

		[SerializeField]
		private Image openedChestImage;

		[SerializeField]
		private Sprite regularSprite;

		[SerializeField]
		private Sprite regularOpenedSprite;

		[SerializeField]
		private Sprite specialSprite;

		[SerializeField]
		private Sprite specialOpenedSprite;

		private int milestoneIndex;

		private Action<int> clicked;

		private Tween pulseTween;

		private int previousRequiredCrystals;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private ItemsProvider itemsProvider;

		private bool blockOpenVisual;

		private bool displayInfo;

		private bool wasClaimedCached;

		private List<RewardBase> rewards;

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x1664c68.
			base.Awake();
			chestButton.onClick.AddListener(OnChestClicked);
		}

		public void Setup(int index, Action<int> onClicked, bool displayInfo)
		{
			// Original APK ARM64 ELF VA 0x1664d04.
			milestoneIndex = index;
			clicked = onClicked;
			rewards = GetMilestoneRewards(index);
			this.displayInfo = displayInfo;
			Initialize();
			SetupRewardsContainer();
			specialItemRewardTxt.gameObject.SetActive(false);
			if (rewards[0] is ItemReward item)
			{
			    specialItemRewardTxt.gameObject.SetActive(true);
			    specialItemRewardTxt.text = LocalizationStrings.Get("EVENT_SPECIAL_ITEM_REWARD_MYTHIC_HELMET").Replace("{amount}", item.Level.ToString());
			}
		}

		private void SetupRewardsContainer()
		{
			// Original APK ARM64 ELF VA 0x1664fc8.
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContent, rewardViewPrefab)
			    .BindTo(Lifetime, () => rewards).Setup((view, reward) =>
			    {
			        view.Setup(reward, RewardViewStyle.Simplified);
			        view.SimplifiedRewardGlow.SetActive(false);
			        view.transform.localScale = Vector3.one;
			        view.SetInfoBtnVisible(displayInfo && reward is ItemReward);
			    });
		}

		private List<RewardBase> GetMilestoneRewards(int milestoneIndex)
		{
			// Original APK ARM64 ELF VA 0x1664ef0.
			var milestones = GetPlanMilestones();
			return milestones == null || milestoneIndex >= milestones.Count ? new List<RewardBase>() :
			    ForecastCrystalRewardEntryHelper.ToRewards(milestones[milestoneIndex].Rewards, itemsProvider);
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			// Original APK ARM64 ELF VA 0x1665154.
			return eventsRepository.EventPlan?.CrystalMilestones;
		}

		public void SetupState(int requiredCrystals, int previousRequiredCrystals, double currentCrystals, bool ready, bool claimed, bool isLast, bool isFirst, RosterDividerState state, bool blockOpenVisual)
		{
			// Original APK ARM64 ELF VA 0x16652b0.
			this.blockOpenVisual = blockOpenVisual;
			thresholdText.text = requiredCrystals.ToString();
			chestButton.interactable = true;
			claimedState.SetActive(claimed);
			readyState.SetActive(!claimed);
			currentCrystalsSection.SetActive(isFirst);
			this.currentCrystals.text = string.Format("{0}", currentCrystals);
			this.previousRequiredCrystals = previousRequiredCrystals;
			readyEffect.SetActive(!blockOpenVisual && ready);
			wasClaimedCached = claimed;
			collectedMarker.SetActive(claimed);
			prevDivider.SetState(state);
			finalDivider.gameObject.SetActive(isLast);
			if (isLast) finalDivider.SetState(currentCrystals >= requiredCrystals ? RosterDividerState.Claimed : RosterDividerState.Empty);
			progressTxt.gameObject.SetActive(false);
			if (currentCrystals >= requiredCrystals) currentProgressSlider.value = 1f;
			else if (currentCrystals <= previousRequiredCrystals) currentProgressSlider.value = 0f;
			else
			{
			    currentProgressSlider.value = (float)((currentCrystals - previousRequiredCrystals) / (requiredCrystals - previousRequiredCrystals));
			    progressTxt.gameObject.SetActive(true);
			    progressTxt.text = string.Format("{0}/{1}", currentCrystals, requiredCrystals);
			}
			chestImage.sprite = isLast ? specialSprite : regularSprite;
			openedChestImage.sprite = isLast ? specialOpenedSprite : regularOpenedSprite;
		}

		private void OnChestClicked()
		{
			// Original APK ARM64 ELF VA 0x166563c.
			if (!blockOpenVisual) clicked?.Invoke(milestoneIndex);
		}

		private void SetPulsing(bool value)
		{
			// Original APK ARM64 ELF VA 0x1665638.
			// Original method is a literal RET (c0035fd6).
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x1665678.
			base.Cleanup();
			rewards = null;
		}

		public void SetTargeted(bool b)
		{
			// Original APK ARM64 ELF VA 0x166569c.
			targetedCanvasGroup.DOKill();
			targetedCanvasGroup.DOFade(b && !wasClaimedCached ? 1f : 0.3f, 0.5f);
		}
	}
}
