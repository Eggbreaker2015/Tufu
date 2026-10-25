using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Rewards;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventEndView : PopupView
	{
		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button claimButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text progressText;

		[Header("Rewards Section")]
		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private TMP_Text rewardText;

		[Header("Progress Bar")]
		[SerializeField]
		private EventProgressBarView progressBarView;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyle;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		private ViewContainer<RewardView, RewardBase> rewardsContainer;

		private ComputedAtom<EventModel> __CompletedEvent;

		private EventModel CompletedEvent => null;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		protected override void Start()
		{
		}

		protected override void Render()
		{
		}

		private void SetupProgressBar()
		{
		}

		private List<RewardBase> GetCurrentRewards()
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
