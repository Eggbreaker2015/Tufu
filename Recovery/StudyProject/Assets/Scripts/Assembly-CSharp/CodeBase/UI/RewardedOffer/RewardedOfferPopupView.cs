using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Shop;
using Codebase.Unit;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.RewardedOffer
{
	public class RewardedOfferPopupView : PopupView
	{
		[SerializeField]
		private Button noThanksButton;

		[SerializeField]
		private Transform lotsRoot;

		[SerializeField]
		private RewardedOfferLotView lotPrefab;

		[SerializeField]
		private Image rewardIcon;

		[SerializeField]
		private TMP_Text rewardValueText;

		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private CashbackModel cashbackModel;

		[Inject]
		private IconStyleConfig iconStyle;

		[Inject]
		private UINavigator navigator;

		private string statTypeKey;

		private StatType statType;

		private double totalRewardValue;

		private double displayedRewardValue;

		private Tween rewardTween;

		private ViewContainer<RewardedOfferLotView, ShopLotModel> lotsContainer;

		public double TotalRewardValue => this.totalRewardValue;

		private List<ShopLotModel> Lots { get; set; }

		protected override void Awake()
		{
		}

		public void Setup(string statTypeKey, double rewardValue)
		{
		}

		private void SetupReceivedReward(double rewardValue)
		{
		}

		private void OnLotPurchased(ShopLotModel lot)
		{
		}

		private void OpenBuyGemsPopup()
		{
		}

		private void OnNoThanksClicked()
		{
		}
	}
}
