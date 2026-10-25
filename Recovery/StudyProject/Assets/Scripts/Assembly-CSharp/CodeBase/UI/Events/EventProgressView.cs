using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Enemy;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player;
using Codebase.Rewards;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventProgressView : PopupView
	{
		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button rewardsInfoButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private TMP_Text timerText;

		[SerializeField]
		private TMP_Text mobCountText;

		[Header("Currencies")]
		[SerializeField]
		private Transform currenciesContainer;

		[SerializeField]
		private EventCurrencyView currencyViewPrefab;

		[SerializeField]
		private CurrencyView getCurrencyView;

		[Header("Progress Bar")]
		[SerializeField]
		private EventProgressBarView progressBarView;

		[SerializeField]
		private TMP_Text bossProgressText;

		[Header("Boosters")]
		[SerializeField]
		private Transform boostersContainer;

		[SerializeField]
		private EventBoosterView boosterViewPrefab;

		[Header("Rewards Section")]
		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private TooltipView rewardsTooltip;

		[Header("Mobs")]
		[SerializeField]
		private Image resistIcon;

		[SerializeField]
		private Image weaponIcon;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventsStyle;

		[Inject]
		private IconStyleConfig iconStyle;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		private ViewContainer<EventCurrencyView, string> currenciesViewContainer;

		private ViewContainer<EventBoosterView, EventBoosterModel> boostersViewContainer;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		protected override void Start()
		{
		}

		public override void Initialize()
		{
		}

		protected override void Render()
		{
		}

		private void UpdateMobCountText(EventModel currentEvent)
		{
		}

		private void ShowRewardsTooltip()
		{
		}

		private void HandleClose()
		{
		}

		private void OpenTutorial()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
