using System.Linq;
using Codebase.Rewards;
using Codebase.Utilities;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.In_apps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventBoosterView : BaseView
	{
		[Header("Common")]
		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private TMP_Text remainingText;

		[Header("States")]
		[SerializeField]
		private GameObject availableState;

		[SerializeField]
		private GameObject soldOutState;

		[Header("Button Sprites")]
		[SerializeField]
		private Sprite gemButtonSprite;

		[SerializeField]
		private Sprite iapButtonSprite;

		[SerializeField]
		private Sprite disabledButtonSprite;

		[SerializeField]
		private Image firstCurrencyImage;

		[SerializeField]
		private Image secondCurrencyImage;

		[SerializeField]
		private TMP_Text firstCurrencyText;

		[SerializeField]
		private TMP_Text secondCurrencyText;

		[Header("VIP")]
		[SerializeField]
		private GameObject vipPointsContainer;

		[SerializeField]
		private TMP_Text vipPointsAmount;

		[Inject]
		private IAPManager iapManager;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		private EventBoosterModel model;

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x165977c.
			base.Awake();
			buyButton.onClick.AddListener(OnBuyClicked);
		}

		public void Setup(EventBoosterModel boosterModel)
		{
			// Original APK ARM64 ELF VA 0x1659818.
			model = boosterModel;
			Initialize();
			Render();
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x165984c.
			if (model == null) return;
			bool available = model.HasRemainingPurchases;
			availableState.SetActive(available);
			soldOutState.SetActive(!available);
			if (!available)
			{
			    buyButton.interactable = false;
			    buyButton.image.sprite = disabledButtonSprite;
			    remainingText.text = string.Empty;
			}
			UpdateRemainingText();
			UpdateNameText();
			if (model.Def.Type == EventBoosterType.Gems) RenderGemBooster();
			else if (model.Def.Type == EventBoosterType.IAP) RenderIAPBooster();
		}

		private void RenderGemBooster()
		{
			// Original APK ARM64 ELF VA 0x1659c68.
			bool canBuy = model.CanPurchaseGemBooster && model.CanSpawnMobs;
			vipPointsContainer.SetActive(false);
			buyButton.interactable = canBuy;
			buyButton.image.sprite = canBuy ? gemButtonSprite : disabledButtonSprite;
			costText.text = string.Format("<sprite name=Gem> {0}", model.CurrentGemCost);
			firstCurrencyImage.sprite = model.Def.FirstIcon;
			secondCurrencyImage.sprite = model.Def.SecondIcon;
			firstCurrencyText.text = secondCurrencyText.text = string.Empty;
			if (!model.CanSpawnMobs) costText.text = LocalizationStrings.Get("EVENT_BOOSTER_SPAWNERS_FULL");
		}

		private void RenderIAPBooster()
		{
			// Original APK ARM64 ELF VA 0x1659e8c.
			bool canBuy = model.CanPurchaseIAPBooster, free = model.HasFreeToken;
			var product = iapManager.GetProductConfig(model.Def.ProductKey);
			vipPointsContainer.SetActive(true);
			vipPointsAmount.text = string.Format("+{0}", product?.inAppValue ?? 0);
			buyButton.interactable = canBuy && (free || iapManager.IsAvailableToPurchase(model.Def.ProductKey));
			buyButton.image.sprite = canBuy ? iapButtonSprite : disabledButtonSprite;
			firstCurrencyImage.sprite = model.Def.FirstIcon;
			secondCurrencyImage.sprite = model.Def.SecondIcon;
			firstCurrencyText.text = (model.Def.Rewards[0] as EventCurrencyReward)?.Amount.ToString("F0");
			secondCurrencyText.text = (model.Def.Rewards[1] as EventCurrencyReward)?.Amount.ToString("F0");
			costText.text = free ? string.Format("<sprite name={0}{1}{0}> 1 <size=80%><color=#BAFFAE>({2})", '"', model.Def.TokenSprite.name, model.FreeTokenCount)
			    : iapManager.GetLocalizedPrice(model.Def.ProductKey);
		}

		private void UpdateRemainingText()
		{
			// Original APK ARM64 ELF VA 0x165995c.
			remainingText.text = model.Def.IsUnlimited ? string.Empty : LocalizationStrings.Get("EVENT_BOOSTER_REMAINING").Replace("{value}", model.RemainingPurchases.ToString());
		}

		private void UpdateNameText()
		{
			// Original APK ARM64 ELF VA 0x1659a68.
			if (model.Def.Type == EventBoosterType.Gems)
			    nameText.text = LocalizationStrings.Get("EVENT_BOOSTER_NAME_" + model.Def.Key).Replace("{value}", model.Def.SpawnCount.ToString());
			else
			{
			    var time = model.Def.Rewards.OfType<EventReward>().FirstOrDefault();
			    nameText.text = time != null ? string.Format("<sprite name={0}{1}{0}> +{2}m", '"', eventStyleConfig.TimeSprite.name, time.ExtraDurationMinutes)
			        : LocalizationStrings.Get("EVENT_BOOSTER_NAME_" + model.Def.Key);
			}
		}

		private void OnBuyClicked()
		{
			// Original APK ARM64 ELF VA 0x165a2b4.
			model?.TryPurchase();
			Render();
		}

		private void LateUpdate()
		{
			// Original APK ARM64 ELF VA 0x165a2e4.
			Render();
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x165a2f0.
			base.Cleanup();
			model = null;
		}
	}
}
