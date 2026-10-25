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
		}

		public void Setup(int index, Action<int> onClicked, bool displayInfo)
		{
		}

		private void SetupRewardsContainer()
		{
		}

		private List<RewardBase> GetMilestoneRewards(int milestoneIndex)
		{
			return null;
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			return null;
		}

		public void SetupState(int requiredCrystals, int previousRequiredCrystals, double currentCrystals, bool ready, bool claimed, bool isLast, bool isFirst, RosterDividerState state, bool blockOpenVisual)
		{
		}

		private void OnChestClicked()
		{
		}

		private void SetPulsing(bool value)
		{
		}

		public override void Cleanup()
		{
		}

		public void SetTargeted(bool b)
		{
		}
	}
}
