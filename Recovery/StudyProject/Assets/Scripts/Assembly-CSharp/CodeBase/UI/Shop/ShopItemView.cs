using Codebase.Utilities;
using UniMob;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using AssetKits.ParticleImage;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Economy;
using Codebase.In_apps;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Services.Purchases;
using Codebase.Services.Purchases.Handlers;
using Codebase.Shop;
using Codebase.Unit;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopItemView : BaseView
	{
		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private Sprite purchaseButtonSprite;

		[SerializeField]
		private Sprite claimButtonSprite;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private ShopItemIconView iconView;

		[SerializeField]
		private TMP_Text amountText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private TMP_Text footerAmountText;

		[SerializeField]
		private ParticleImage buyVfx;

		[SerializeField]
		private GameObject notification;

		[SerializeField]
		private GameObject lockedState;

		[SerializeField]
		private TMP_Text lockedText;

		[SerializeField]
		private RectTransform vipPointsContainer;

		[SerializeField]
		private TMP_Text vipPointsAmount;

		[SerializeField]
		private ShopItemRarityContainer rarityContainer;

		[SerializeField]
		private LayoutElement layoutElement;

		private StatType statType;

		private double cost;

		private double value;

		private ShopLotType lotType;

		private ProductKey productKey;

		[Inject]
		private IconStyleConfig iconStyle;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;

		[Inject]
		private UserModel userModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		[Inject]
		private PurchaseProcessingService purchaseService;

		[Inject]
		private ChestsConfig chestsConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private IAPManager iapManager;

		private ShopLotModel model;

		private ChestDef chestDef;

		private float updateTimer;

		private float defaultHeight;

		[CompilerGenerated]
		private static Action<GemsReceivedSignal> m_OnGemsReceived;

		private ComputedAtom<double> __TotalValue;

		public float PreferredHeight => layoutElement.preferredHeight; // Native 0x16272d4.

		private double TotalValue {
            get {
                // Native 0x16272f4.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__TotalValue == null) __TotalValue = CodeGenAtom.CreatePooled(this, null, () => TotalValue, false);
                return __TotalValue.DirectEvaluate() ? model.TotalValue : __TotalValue.Value;
            }
        }

		private string prefix {
            get {
                // Native 0x1627420 and predicate 0x1628938.
                if (lotType == ShopLotType.Currency || lotType == ShopLotType.Soul) return string.Empty;
                if (lotType == ShopLotType.Gems) return "<sprite name=Gem>";
                return "<sprite name=" + iconStyle.Styles.First(style => style.StatType == statType).Sprite.name + "> +";
            }
        }

		private bool IsBigGemsSlot => (uint)((int)productKey - 4) < 3; // Native 0x1627574.

		public static event Action<GemsReceivedSignal> OnGemsReceived
		{
            add { m_OnGemsReceived += value; }
            remove { m_OnGemsReceived -= value; }
		}

		protected override void Awake()
		{
            // Native 0x1627588.
            base.Awake(); defaultHeight = layoutElement.preferredHeight;
		}

		public void Setup(ShopLotDef def, ShopLotModel model)
		{
            // Native 0x16275c0; original ARM64 float-register tracking preserves authored height/position offsets.
            statType = def.StatType; cost = def.Cost; value = def.Value; lotType = def.LotType; productKey = def.ProductKey;
            this.model = model;
            SubscribeToGemsDelivery();
            if (lotType == ShopLotType.Gems) {
                icon.enabled = true; icon.sprite = def.Sprite; icon.rectTransform.localScale = Vector3.one;
                icon.SetNativeSize(); amountText.text = prefix + TotalValue.Format();
            }
            if (lotType == ShopLotType.Currency) {
                icon.enabled = false; iconView.Render(def.IconType, currencyStyleConfig.GetIcon(def.CurrencyKey));
                amountText.text = prefix + TotalValue.Format();
            }
            if (lotType == ShopLotType.Soul) {
                icon.enabled = false; iconView.Render(def.IconType, soulStyleConfig.GetIcon(def.SoulKey));
                amountText.text = prefix + TotalValue.Format();
            }
            float height = defaultHeight;
            if (lotType == ShopLotType.Gems) {
                var product = GameContext.Get<IAPManager>().GetProductConfig(def.ProductKey);
                int points = product == null ? 0 : product.inAppValue;
                vipPointsContainer.gameObject.SetActive(true);
                vipPointsAmount.text = string.Format("+{0}", points);
                float extra = vipPointsContainer.rect.height;
                height = height + (extra + 10f);
                Vector2 position = icon.rectTransform.anchoredPosition;
                position.y = position.y + vipPointsContainer.rect.height * -.5f;
                icon.rectTransform.anchoredPosition = position;
            } else vipPointsContainer.gameObject.SetActive(false);
            if (lotType == ShopLotType.Chest) {
                chestDef = chestsConfig.Get(def.ChestKey);
                rarityContainer.SetupChestDrops(chestDef);
                rarityContainer.gameObject.SetActive(true);
                height = height + rarityContainer.rectTransform.sizeDelta.y;
            } else {
                footerAmountText.text = string.Empty;
                rarityContainer.gameObject.SetActive(false);
            }
            layoutElement.preferredHeight = height;
            Atom.Reaction(Lifetime, Render);
            Initialize(); Render();
		}

		private void RaiseGemsReceived()
		{
            // Native 0x1627d44. Pure UI signal does not grant currency; actual delivery is handled elsewhere.
            if (lotType != ShopLotType.Gems) return;
            audioPlayer.PlaySoundOnce(IsBigGemsSlot ? "shop_gems_big" : "shop_gems_small");
            int count = Math.Min((int)Math.Sqrt(value / 20.0), 100);
            m_OnGemsReceived?.Invoke(new GemsReceivedSignal {SourceRect = rectTransform, VisualCount = count});
		}

		protected override void Render()
		{
            // Native 0x1627ef8: distinct Gems, Currency/Soul and Stat/Chest branches preserve their original early returns.
            if (lotType == ShopLotType.Gems) notification.SetActive(false);
            else if (lotType == ShopLotType.Currency || lotType == ShopLotType.Soul) {
                notification.SetActive(model.CanClaim);
                buyButton.image.sprite = model.CanClaim ? claimButtonSprite : purchaseButtonSprite;
                costText.text = model.CanClaim ? LocalizationStrings.Get("SHOP_CLAIM") : "<sprite name=Gem>" + cost.ToString();
                amountText.text = prefix + TotalValue.Format();
                icon.enabled = false;
                Sprite sprite = lotType == ShopLotType.Soul ? soulStyleConfig.GetIcon(model.Def.SoulKey) : currencyStyleConfig.GetIcon(model.Def.CurrencyKey);
                iconView.Render(model.Def.IconType, sprite);
            } else {
                bool unlocked = true;
                if (lotType == ShopLotType.Chest) {
                    unlocked = model.Def.RequiredLocationNumber <= playerModel.LastUnlockedLocation.Number;
                    lockedState.SetActive(!unlocked);
                    lockedText.text = LocalizationStrings.Get("SHOP_UNLOCKS_AT_ISLAND").Replace("{number}", model.Def.RequiredLocationNumber.ToString());
                }
                buyButton.interactable = unlocked;
                buyButton.image.sprite = model.CanClaim ? claimButtonSprite : purchaseButtonSprite;
                notification.SetActive(model.CanClaim);
                costText.text = model.CanClaim ? LocalizationStrings.Get("SHOP_CLAIM") : "<sprite name=Gem>" + cost.ToString();
                amountText.text = prefix + TotalValue.Format();
                var style = iconStyle.Styles.First(item => item.StatType == statType);
                Sprite sprite;
                if (model.Def.LotType == ShopLotType.Chest) {
                    footerAmountText.text = "x" + model.TotalValue.Format();
                    amountText.text = LocalizationStrings.Get("SHOP_ITEM_NAME_" + model.Key);
                    sprite = chestDef.ClosedSprite;
                } else sprite = style.Sprite;
                icon.enabled = false;
                iconView.Render(model.Def.IconType, sprite);
                return;
            }
            if (rarityContainer != null) rarityContainer.gameObject.SetActive(false);
		}

		private void LateUpdate()
		{
            // Native 0x16285e8: local store state and cached localized price only; no purchase starts here.
            if (lotType != ShopLotType.Gems) return;
            buyButton.interactable = iapManager.IsAvailableToPurchase(productKey);
            costText.text = iapManager.GetLocalizedPrice(productKey);
		}

		public void TryPurchase()
		{
            // Native 0x1628674; this action is never invoked by the paid-lot UI validation.
            if (lotType == ShopLotType.Gems) { model.Purchase(); return; }
            if (!model.CanPurchase) { OpenBuyGemsPopup(); return; }
            model.Purchase();
            buyVfx.Play();
		}

		private void OpenBuyGemsPopup()
		{
            // Native 0x16286dc; secondary popup remains a separate recovery scope.
            GameContext.Get<UINavigator>().Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab").SetupAsGems(cost);
		}

		private void SubscribeToGemsDelivery()
		{
            // Native 0x1627c00.
            if (productKey == ProductKey.None || lotType != ShopLotType.Gems) return;
            PurchasePopupController.OnPurchaseClaimed += HandleGemsDelivered;
		}

		private void HandleGemsDelivered(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
		{
            // Native 0x1628794.
            if (rewardInfo.ProductKey == productKey) RaiseGemsReceived();
		}

		protected override void OnDestroy()
		{
            // Native 0x16287ac: lifetime disposal precedes original static delivery unsubscription.
            base.OnDestroy();
            PurchasePopupController.OnPurchaseClaimed -= HandleGemsDelivered;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
            // Native 0x16288dc.
            m_OnGemsReceived = null;
		}
	}
}
