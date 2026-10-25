using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.UI.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
    public class PurchaseRewardView : PopupView
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button closeButton;
        [Inject] private ShopSectionsConfig shopConfig;
        [Inject] private EventsProvider eventsProvider;

        // Original ARM64 ELF VA 0x15f8b6c, 0x15f8c08, 0x15f8cc0, 0x15f8cec.
        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }
        public void Setup(Sprite sprite, string amount)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            amountText.text = amount;
        }
        public void Setup(string rewardType, string amount) => Setup(ResolveIcon(rewardType), amount);
        private Sprite ResolveIcon(string rewardType)
        {
            if (rewardType == "gems")
                return shopConfig.Sections.SelectMany(section => section.Lots).FirstOrDefault(lot => lot.LotType == ShopLotType.Gems)?.Sprite;
            if (rewardType == "eventcurrency")
                return eventsProvider.Events.Where(config => config.Boosters != null).SelectMany(config => config.Boosters).FirstOrDefault(booster => booster.Type == EventBoosterType.IAP)?.TokenSprite;
            return null;
        }
    }
}
