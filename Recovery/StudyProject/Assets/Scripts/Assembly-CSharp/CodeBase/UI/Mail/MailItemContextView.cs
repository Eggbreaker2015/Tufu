using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Mail;
using Codebase.Services.Time;
using Codebase.UI.Events;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Mail
{
	public class MailItemContextView : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text titleField;

		[SerializeField]
		private TMP_Text descriptionField;

		[SerializeField]
		private TMP_Text relativeDateField;

		[SerializeField]
		private TMP_Text absoluteDateField;

		[SerializeField]
		private Button claimButton;

		[SerializeField]
		private Button reopenPresentationButton;

		[SerializeField]
		private MailAttachmentView attachmentView;

		[SerializeField]
		private Transform statRewardsContent;

		[SerializeField]
		private RewardView statRewardViewPrefab;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		[Inject]
		private MailsRepository mailsRepository;

		[Inject]
		private EventsProvider eventsProvider;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private UINavigator navigator;

		private MailModel model;

		private readonly List<RewardView> spawnedStatRewardViews;

		private Tween closeTween;

		protected override void Awake()
		{
		}

		private void ReopenPresentation()
		{
		}

		private void HandleClose()
		{
		}

		private void OnEnable()
		{
		}

		private void Claim()
		{
		}

		private void Close()
		{
		}

		public void Open(MailModel model)
		{
		}

		protected override void Render()
		{
		}

		private Sprite FindTokenIcon(string storeId)
		{
			return null;
		}

		private void UpdateRewardClaimedState()
		{
		}

		private void RenderRewards()
		{
		}
	}
}
