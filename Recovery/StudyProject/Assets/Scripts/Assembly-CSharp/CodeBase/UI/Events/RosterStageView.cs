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

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		protected override void Start()
		{
		}

		public void Setup(int stageIndex, bool isLast, Action onExpandRequested = null, Action onCollapseRequested = null, bool forceShowAsPassed = false, bool hideCheck = false)
		{
		}

		public void UpdateExpandState(bool isCurrentRow, bool isExpanded)
		{
		}

		private void SetupDivider(bool isLast, bool isPassed)
		{
		}

		private void SetupRewardsContainer()
		{
		}

		private static bool AreRewardsEqual(RewardBase a, RewardBase b)
		{
			return false;
		}

		protected override void Render()
		{
		}

		private List<RewardBase> GetStageRewardsWithoutEventReward()
		{
			return null;
		}

		private EventReward GetEventReward()
		{
			return null;
		}

		private List<RewardBase> GetStageRewards()
		{
			return null;
		}

		public override void Cleanup()
		{
		}
	}
}
