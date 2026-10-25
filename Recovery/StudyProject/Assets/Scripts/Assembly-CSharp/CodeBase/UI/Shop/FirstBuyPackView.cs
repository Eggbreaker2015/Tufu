using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.Shop;
using Codebase.UI.Events;
using Codebase.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class FirstBuyPackView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button goToShopButton;

		[SerializeField]
		private Button claimButton;

		[SerializeField]
		private GameObject rewardsContainer;

		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private List<InventoryItemView> itemViews;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private UserModel userModel;

		[Inject]
		private FirstBuyPackRewardsConfig rewardsConfig;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		protected override void Awake()
		{
		}

		protected override void Start()
		{
		}

		private void SetupItemViews()
		{
		}

		private void OpenShop()
		{
		}

		private void ClaimRewards()
		{
		}

		protected override void Render()
		{
		}
	}
}
