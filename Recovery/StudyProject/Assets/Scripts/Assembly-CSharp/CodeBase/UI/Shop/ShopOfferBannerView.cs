using Codebase.Localization;
using Codebase.Utilities;
using System;
using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.In_apps;
using Codebase.Services.Purchases;
using Codebase.Services.Time;
using Multicast.UserData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopOfferBannerView : View
	{
		[SerializeField]
		private ProductKey productKey;

		[SerializeField]
		private TMP_Text headerText;

		[SerializeField]
		private GameObject content;

		[SerializeField]
		private GameObject purchasedLabel;

		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text priceText;

		[SerializeField]
		private TMP_Text vipPointsText;

		[SerializeField]
		private TMP_Text durationText;

		[SerializeField]
		private GameObject weeklyLimitState;

		[SerializeField]
		private TMP_Text weeklyLimitText;

		[Header("Bonuses")]
		[SerializeField]
		private Transform bonusesRoot;

		[SerializeField]
		private ShopOfferRewardItemView bonusItemPrefab;

		[Inject]
		private UserModel userModel;

		[Inject]
		private PurchaseProcessingService purchaseService;

		[Inject]
		private IAPManager iapManager;

		[Inject]
		private ITimeService timeService;

		private const double SecondsPerDay = 86400.0;

		private bool purchaseStarted;

		private UdValue<bool> purchasedFlag;

		private UdValue<double> purchaseTime;

		private int durationDays;

		private bool weeklyMode;

		private Func<double> weeklyResetTimeProvider;

		private ShopOfferType offerType;

		private readonly List<ShopOfferRewardItemView> spawnedBonusItems = new List<ShopOfferRewardItemView>(); // Native ctor 0x1629a38.

		public void SetOfferType(ShopOfferType type)
		{
            // Native 0x1628978.
            offerType = type; RefreshHeader();
		}

		public void SetPurchasedFlag(UdValue<bool> flag)
		{
            // Native 0x16289b8.
            purchasedFlag = flag;
		}

		public void SetDuration(UdValue<double> purchaseTime, int durationDays)
		{
            // Native 0x16289c0.
            this.purchaseTime = purchaseTime; this.durationDays = durationDays;
		}

		public void SetWeeklyReset(Func<double> nextResetUnixSeconds)
		{
            // Native 0x16289ec.
            weeklyResetTimeProvider = nextResetUnixSeconds; weeklyMode = true;
		}

		public void SetBonuses(IReadOnlyList<ShopOfferBonusData> bonuses)
		{
            // Native 0x16289fc.
            foreach (var item in spawnedBonusItems) Destroy(item.gameObject);
            spawnedBonusItems.Clear();
            foreach (var bonus in bonuses) {
                var item = Instantiate(bonusItemPrefab, bonusesRoot);
                item.Setup(bonus);
                spawnedBonusItems.Add(item);
            }
		}

		protected override void Start()
		{
            // Native 0x1629060; only local product configuration is read during setup.
            base.Start();
            var product = iapManager.GetProductConfig(productKey);
            if (product != null) vipPointsText.text = string.Format("+{0}", product.inAppValue);
            button.onClick.AddListener(OnButtonClicked);
		}

		private void OnEnable()
		{
            // Native 0x1629184.
            purchaseService.OnPurchaseFinished += OnPurchaseFinished;
            LocalizationService.OnLocaleChanged += RefreshHeader;
            RefreshHeader(); Initialize();
		}

		private void RefreshHeader()
		{
            // Native 0x1628980.
            headerText.text = LocalizationStrings.Get(GetHeaderKey());
		}

		private string GetHeaderKey()
		{
            // Native 0x1629294, original three-way rodata table.
            switch (offerType) {
                case ShopOfferType.SoulPass: return "SHOP_BANNER_SOUL_OFFER";
                case ShopOfferType.CrystalPass: return "SHOP_BANNER_CRYSTAL_OFFER";
                case ShopOfferType.ChaoticSoul: return "SHOP_BANNER_CHAOTIC_SOUL_OFFER";
                default: return string.Empty;
            }
		}

		protected override void Render()
		{
            // Native 0x1629314.
            bool purchased = purchasedFlag != null && purchasedFlag.Value;
            if (weeklyMode) { purchasedLabel.SetActive(false); RenderWeeklyState(); }
            else { button.gameObject.SetActive(!purchased); purchasedLabel.SetActive(purchased); weeklyLimitState.SetActive(false); }
            priceText.text = iapManager.GetLocalizedPrice(productKey);
		}

		private void Update()
		{
            // Native 0x162968c.
            if (weeklyMode) RenderWeeklyState(); else RenderDuration();
		}

		private void RenderWeeklyState()
		{
            // Native 0x1629424; the weekly time provider is read only for an already purchased offer.
            bool purchased = purchasedFlag != null && purchasedFlag.Value;
            button.gameObject.SetActive(!purchased);
            weeklyLimitState.SetActive(true);
            weeklyLimitText.text = LocalizationStrings.Get(purchased ? "SHOP_BANNER_CHAOTIC_SOUL_LIMIT_PURCHASED" : "SHOP_BANNER_CHAOTIC_SOUL_LIMIT");
            durationText.transform.parent.gameObject.SetActive(purchased);
            if (!purchased) return;
            long now = new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
            durationText.text = TimeLocalizer.GetFormatTime((float)(weeklyResetTimeProvider() - now), true);
		}

		private void RenderDuration()
		{
            // Native 0x162969c; original elapsed calculation uses double seconds/day.
            if (purchasedFlag == null || !purchasedFlag.Value)
                durationText.text = LocalizationStrings.Get("SHOP_BANNER_DURATION").Replace("{value}", durationDays.ToString());
            else {
                long now = new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
                durationText.text = TimeLocalizer.GetFormatTime((float)(purchaseTime.Value + durationDays * SecondsPerDay - now), true);
            }
		}

		private void OnButtonClicked()
		{
            // Native 0x16298c0. This real purchase branch is restored but never invoked by validation.
            if (purchaseStarted) return;
            purchaseStarted = true; button.interactable = false;
            purchaseService.StartPurchase(productKey);
		}

		private void OnPurchaseFinished()
		{
            // Native 0x1629910.
            purchaseStarted = false; button.interactable = true;
		}

		public override void Cleanup()
		{
            // Native 0x1629934.
            base.Cleanup();
            purchaseService.OnPurchaseFinished -= OnPurchaseFinished;
            LocalizationService.OnLocaleChanged -= RefreshHeader;
		}
	}
}
