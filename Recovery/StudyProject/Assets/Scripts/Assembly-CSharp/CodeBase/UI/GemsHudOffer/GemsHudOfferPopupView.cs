using Codebase.UI.Shop;
using Codebase.Analytics.Events;
using Codebase.Utilities;
using Codebase.Unit;
using System.Collections.Generic;
using System.Linq;
using System;
using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Integrations.Ads;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.GemsHudOffer
{
	public class GemsHudOfferPopupView : PopupView
	{
		private static readonly GemsHudOfferData Data = new GemsHudOfferData(); // Native cctor16571bc.

		[SerializeField]
		private Image statIcon;

		[SerializeField]
		private TMP_Text statValueText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private TMP_Text descriptionText;

		[SerializeField]
		private TMP_Text discountText;

		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private Button closeButton;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private CashbackModel cashbackModel;

		[Inject]
		private IconStyleConfig iconStyle;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private StatRewardContainer statRewardContainer;

		[Inject]
		private GemsHudOfferModel gemsHudOfferModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		private Codebase.Integrations.Ads.GemsHudOffer offer;

		protected override void Awake()
		{
            // Native ELF0x1656414.
            base.Awake(); closeButton.onClick.AddListener(Close); buyButton.onClick.AddListener(OnBuyClicked);
        }

		public override void Cleanup()
		{
            // Native ELF0x1656508.
            base.Cleanup(); gemsHudOfferModel.Paused = false;
        }

		public void Setup(Codebase.Integrations.Ads.GemsHudOffer offer)
		{
            // Native ELF0x1656530.
            this.offer = offer; gemsHudOfferModel.Paused = true;
            var style = iconStyle.Styles.FirstOrDefault(x => x.StatType == offer.StatType); if (style != null) { statIcon.sprite = style.Sprite; statIcon.enabled = true; }
            statValueText.text = offer.Value.Format(); int discount = Data.fixedDiscountInUi ? Data.fixedDiscountPercent : offer.DiscountPercent;
            int oldCost = Data.fixedDiscountInUi ? NativeInt(Math.Round((double)offer.Cost / ((double)discount / -100.0 + 1.0))) : offer.BaseCost;
            costText.text = string.Format("<color=#D0FFC7><size=80%><s> {0}  </s> </color><size=100%>{1}<sprite name=gem>", oldCost, offer.Cost);
            descriptionText.text = LocalizationStrings.Get("REWARDED_GEM_OFFER_VALUE").Replace("{value}", discount.ToString()); descriptionText.gameObject.SetActive(Data.fixedDiscountInUi || discount > 0);
            discountText.text = Data.fixedDiscountInUi || discount > 0 ? string.Format("-{0}%", discount) : string.Empty; discountText.gameObject.SetActive(Data.fixedDiscountInUi || discount > 0);
        }

		private void OnBuyClicked()
		{
            // Native ELF0x1656b6c.
            if (offer == null) return;
            if (playerModel.Gems < (double)offer.Cost) { navigator.Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab").SetupAsGems(); return; }
            playerModel.Gems -= offer.Cost; analytics.Send(new HardCurrencySpentEvent("gems_hud_offer", offer.Cost, new Dictionary<string, object> { ["stat_type"] = offer.StatType.ToString(), ["value"] = offer.Value }));
            var modifier = new StatModifier { Name = "shop.gems_hud_offer", StatType = offer.StatType, ModifierType = ModifierType.Flat, Source = ModifierSource.Shop, Value = (float)offer.Value * (float)cheatsModel.DropMultiplier };
            Stat stat = playerModel.Unit.Stats.First(x => x.Type == offer.StatType); var style = iconStyle.Styles.First(x => x.StatType == offer.StatType);
            stat.AddModifier(modifier); cashbackModel.AddEarnedStat(modifier); statRewardContainer.Create("<color=green>+" + ((double)modifier.Value).Format() + "<sprite name=" + style.Sprite.name + ">", false);
            gemsHudOfferModel.Hide(); Close();
        }
        // ARM FCVTZS plus explicit original +Infinity guard; normal inputs are original Math.Round outputs.
        private static int NativeInt(double value) => double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;

	}
}
