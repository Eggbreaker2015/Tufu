using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.In_apps;
using Codebase.LevelPass;
using Codebase.Rewards;
using Codebase.Services.Purchases;
using Codebase.Services.Purchases.Handlers;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.LevelPass
{
	public class LevelPassPurchaseView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private TMP_Text buyButtonText;

		[SerializeField]
		private TMP_Text rewardsText;

		[SerializeField]
		private TMP_Text passTitle;

		[SerializeField]
		private TMP_Text passLabel;

		[SerializeField]
		private TMP_Text vipPointsText;

		[SerializeField]
		private Transform islandsContainer;

		[SerializeField]
		private LevelPassIslandTextView islandLabelPrefab;

		[Inject]
		private PurchaseProcessingService purchaseService;

		[Inject]
		private IAPManager iapManager;

		private LevelPassDef levelPassDef;

		protected override void Awake()
		{
		}

		public void Setup(LevelPassDef def, int passNumber)
		{
		}

		public override void Cleanup()
		{
		}

		private void HandlePurchaseDelivered(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
		{
		}

		private void LateUpdate()
		{
		}

		private void UpdateVipPoints()
		{
		}

		private void UpdatePrice()
		{
		}

		private void RenderIslands(LevelPassDef def)
		{
		}

		private Dictionary<StatType, double> CollectPreAdditivePostMultiplicativeStats(LevelPassDef def)
		{
			return null;
		}

		private void CollectStatsFromReward(RewardBase reward, Dictionary<StatType, double> totals)
		{
		}

		private double CountTotalGems(LevelPassDef def)
		{
			return 0.0;
		}

		private double CountGemsInReward(RewardBase reward)
		{
			return 0.0;
		}

		private void OnBuyClicked()
		{
		}
	}
}
