using Codebase.Core;
using Codebase.Utilities;
using System;
using System.Runtime.CompilerServices;
using Codebase.Core.UI;
using Codebase.In_apps;
using Codebase.Services.Purchases;
using Codebase.Services.Purchases.Handlers;
using Codebase.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class BuyGemsLotView : View
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text amountText;

		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private Sprite purchaseButtonSprite;

		[SerializeField]
		private Sprite claimButtonSprite;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private TMP_Text vipPointsText;

		[SerializeField]
		private GameObject vipBackground;

		private ShopLotModel model;

		private bool isCurrencyMode;

		private bool isInteractable = true;

		private IAPManager iapManager;

		public RectTransform IconRect => icon.rectTransform;

		public Sprite IconSprite => icon.sprite;

		public event Action<BuyGemsLotView> OnPurchased;

		public event Action<double> OnGemsInsufficient;

		public void Setup(ShopLotModel lot)
		{
            // Original ELF 0x1618ae0.
            ResetState(); model = lot; isCurrencyMode = false;
            iapManager = GameContext.Get<IAPManager>();
            icon.sprite = lot.Def.Sprite; icon.enabled = true; icon.SetNativeSize();
            amountText.text = "<sprite name=Gem>" + lot.Def.Value.ToString();
            vipPointsText.text = string.Format("+{0}", iapManager.GetProductConfig(lot.Def.ProductKey)?.inAppValue ?? 0);
            vipBackground.SetActive(true);
            buyButton.onClick.AddListener(HandleBuyClicked);
            PurchasePopupController.OnPurchaseClaimed += HandleDelivered;
            Initialize(); Render();
		}

		public void SetupAsCurrencyLot(ShopLotModel lot, Sprite currencyIcon)
		{
            // Original ELF 0x1618fc4.
            ResetState(); model = lot; isCurrencyMode = true;
            icon.sprite = currencyIcon; icon.enabled = currencyIcon != null;
            if (currencyIcon != null) icon.SetNativeSize();
            amountText.text = lot.TotalValue.Format(); vipPointsText.text = string.Empty; vipBackground.SetActive(false);
            buyButton.onClick.AddListener(HandleBuyClicked);
            Initialize(); Render();
		}

		protected override void Render()
		{
            // Original ELF 0x16191e0: currency offers remain clickable to open the insufficient-gems popup.
            if (isCurrencyMode)
            {
                buyButton.interactable = isInteractable;
                buyButton.image.sprite = model.CanClaim ? claimButtonSprite : purchaseButtonSprite;
                costText.text = model.CanClaim ? LocalizationStrings.Get("SHOP_CLAIM") : "<sprite name=Gem>" + model.GemCost.Format();
            }
            else
            {
                buyButton.interactable = isInteractable && iapManager.IsAvailableToPurchase(model.Def.ProductKey);
                costText.text = iapManager.GetLocalizedPrice(model.Def.ProductKey);
            }
		}

		public void SetInteractable(bool value)
		{
            // Original ELF 0x16193cc.
            isInteractable = value;
            buyButton.interactable = isCurrencyMode ? value : value && iapManager.IsAvailableToPurchase(model.Def.ProductKey);
		}

		private void HandleBuyClicked()
		{
            // Original ELF 0x1619434. Platform purchase is delegated unchanged; validation never clicks this branch.
            if (isCurrencyMode) HandleCurrencyBuy();
            else model.Purchase();
		}

		private void HandleCurrencyBuy()
		{
            // Original ELF 0x1619458: B.PL lets unordered values reach Purchase, retaining the original local boundary.
            if (!model.CanClaim && GameContext.Get<global::Codebase.Player.Models.PlayerModel>().Gems < model.GemCost)
            {
                OnGemsInsufficient?.Invoke(model.GemCost);
                return;
            }
            model.Purchase(); OnPurchased?.Invoke(this);
		}

		private void HandleDelivered(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
		{
            // Original ELF 0x1619584. deliveryInfo is unused; no synthetic delivery is generated here.
            if (rewardInfo.ProductKey == model.Def.ProductKey) OnPurchased?.Invoke(this);
		}

		private void ResetState()
		{
            // Original ELF 0x1618e58.
            buyButton.onClick.RemoveAllListeners();
            PurchasePopupController.OnPurchaseClaimed -= HandleDelivered;
            OnPurchased = null; OnGemsInsufficient = null; isInteractable = true;
		}

		public override void Cleanup()
		{
            // Original ELF 0x16195d4.
            base.Cleanup(); ResetState(); isCurrencyMode = false;
		}
	}
}
