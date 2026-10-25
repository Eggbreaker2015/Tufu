using System.Collections.Generic;
using System.Linq;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Shop;
using Codebase.Shop.Commands;
using Codebase.UI.Events;
using Codebase.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
    public class FirstBuyPackView : PopupView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button goToShopButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject rewardsContainer;
        [SerializeField] private Transform rewardsContent;
        [SerializeField] private RewardView rewardViewPrefab;
        [SerializeField] private List<InventoryItemView> itemViews;
        [Inject] private UINavigator navigator;
        [Inject] private UserModel userModel;
        [Inject] private FirstBuyPackRewardsConfig rewardsConfig;
        private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

        // Original ARM64 ELF VA 0x1623c9c, 0x1623de0, 0x1623fe4, 0x16241d8, 0x1624250, 0x16242d8.
        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
            goToShopButton.onClick.AddListener(OpenShop);
            claimButton.onClick.AddListener(ClaimRewards);
        }
        protected override void Start()
        {
            base.Start();
            rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContent, rewardViewPrefab)
                .BindTo(Lifetime, () => rewardsConfig.Rewards.ToList())
                .Setup((view, reward) => view.Setup(reward));
            SetupItemViews();
        }
        private void SetupItemViews()
        {
            var items = rewardsConfig.Rewards.OfType<ItemReward>().ToList();
            for (int index = 0; index < itemViews.Count; index++)
            {
                var view = itemViews[index];
                if (index < items.Count)
                {
                    var item = new ItemModel(Lifetime, items[index].Def);
                    item.SetInitialCopies(items[index].Level);
                    view.Initialize(item, false, false);
                    view.gameObject.SetActive(true);
                }
                else view.gameObject.SetActive(false);
            }
        }
        private void OpenShop()
        {
            navigator.Open<ShopView>("Assets/Prefabs/UI/Shop.prefab", false);
            Close();
        }
        private void ClaimRewards()
        {
            App.ExecuteCommand(new TryGrantFirstBuyPackRewardsCommand());
            Close();
        }
        protected override void Render()
        {
            bool paid = userModel.InAppsBoughtValueInRewarded.Value > 0f;
            goToShopButton.gameObject.SetActive(!paid);
            claimButton.gameObject.SetActive(paid);
            // Local learning guard: a pooled view cannot offer a second claim after redemption.
            claimButton.interactable = paid && !userModel.FirstBuyPackRewardsGranted.Value;
        }
    }
}
