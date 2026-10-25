using Codebase.Meta.Upgrades;
using Codebase.UI.Shop;
using Codebase.Economy;
using Codebase.Utilities;
using System.Threading;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using BrunoMikoski.AnimationSequencer;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletUpgradeNodeTooltipView : View
	{
		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text descriptionText;

		[SerializeField]
		private TMP_Text levelText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private Image iconImage;

		[SerializeField]
		private SoulView soulView;

		[SerializeField]
		private Button upgradeButton;

		[SerializeField]
		private AnimationSequencerController upgradeButtonAnimation;

		[SerializeField]
		private TMP_Text experienceText;

		[SerializeField]
		private Button openInBuilderButton;

		[SerializeField]
		private GameObject costContainer;

		[SerializeField]
		private GameObject lockedState;

		[SerializeField]
		private TMP_Text lockedText;

		[SerializeField]
		private GameObject maxedState;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private GameObject descriptionBg;

		[SerializeField]
		private TMP_Text additionalDescriptionText;

		[Inject]
		private AmuletUpgradeConfig config;

		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private ShopLotRepository shopLotRepository;

		[CompilerGenerated]
		private Action m_OnUpgradeClicked;

		[CompilerGenerated]
		private Action m_OnBuyCurrencyPopupClosed;

		private static readonly Color ExperienceAvailableColor = new Color(0.7369999885559082f, 0.3449999988079071f, 0.0f, 1.0f);

		private static readonly Color ExperienceLockedColor = new Color(0.32899999618530273f, 0.3959999978542328f, 0.45500001311302185f, 1.0f);

		private string upgradeKey;

		public event Action OnUpgradeClicked
        {
            // Native ELF VA 0x13d5724/0x13d54b8.
            add { Action oldValue, newValue; do { oldValue = m_OnUpgradeClicked; newValue = (Action)Delegate.Combine(oldValue,value); } while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnUpgradeClicked,newValue,oldValue),oldValue)); }
            remove { Action oldValue, newValue; do { oldValue = m_OnUpgradeClicked; newValue = (Action)Delegate.Remove(oldValue,value); } while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnUpgradeClicked,newValue,oldValue),oldValue)); }
        }

		public event Action OnBuyCurrencyPopupClosed
        {
            // Native ELF VA 0x13d57c0/0x13d5554.
            add { Action oldValue, newValue; do { oldValue = m_OnBuyCurrencyPopupClosed; newValue = (Action)Delegate.Combine(oldValue,value); } while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnBuyCurrencyPopupClosed,newValue,oldValue),oldValue)); }
            remove { Action oldValue, newValue; do { oldValue = m_OnBuyCurrencyPopupClosed; newValue = (Action)Delegate.Remove(oldValue,value); } while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnBuyCurrencyPopupClosed,newValue,oldValue),oldValue)); }
        }

		protected override void Awake()
		{
			// Native ELF VA 0x13d6f84.
			base.Awake(); upgradeButton.onClick.AddListener(HandleUpgradeClick); infoButton.onClick.AddListener(ToggleAdditionalDescription);
			openInBuilderButton.gameObject.SetActive(false); Hide();
		}

		public void Setup(string upgradeKey, Sprite icon)
		{
			// Native ELF VA 0x13d55f0; Initialize precedes immediate Render.
			this.upgradeKey = upgradeKey; iconImage.sprite = icon;
			nameText.text = LocalizationStrings.Get("UPGRADE_" + upgradeKey.ToUpperInvariant());
			additionalDescriptionText.text = LocalizationStrings.Get("GRAPH_ZERO_COOLDOWN_DESCRIPTION");
			descriptionBg.SetActive(false); Initialize(); Render();
		}

		public void Show()
		{
			// Native ELF VA 0x13d58c4.
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			// Native ELF VA 0x13d6444.
			gameObject.SetActive(false);
		}

		protected override void Render()
		{
			// Native ELF VA 0x13d70a0.
			if (string.IsNullOrEmpty(upgradeKey)) return;
			AmuletUpgradeDef definition = config.GetUpgrade(upgradeKey); if (definition == null) return;
			int level = upgradesModel.GetLevel(upgradeKey), max = upgradesModel.GetMaxLevel(upgradeKey);
			bool available = upgradesModel.IsAvailable(upgradeKey), isMaxed = level >= max, canAfford = upgradesModel.CanAfford(upgradeKey);
			if (!available && !isMaxed) { levelText.text = @"<sprite name=""atlas_93""> Locked"; lockedText.text = LocalizationStrings.Get("AMULET_UPGRADE_LOCKED").Replace("{value}",definition.RequiredAmuletLevel.ToString()); }
			else levelText.text = string.Format("Lv. {0}/{1}",level,max);
			costContainer.SetActive((available || isMaxed) && !isMaxed); lockedState.SetActive(!available && !isMaxed); maxedState.SetActive(isMaxed);
			UpdateAdditionalDescription(); UpdateDescription(); UpdateCostDisplay(isMaxed,canAfford); UpdateUpgradeButton(isMaxed,canAfford);
		}

		private void UpdateDescription()
		{
			// Native ELF VA 0x13d749c.
			if (descriptionText == null) return;
			descriptionText.text = new AmuletUpgradeDescriptionContext(upgradeKey,upgradesModel,false)
			    .Resolve(LocalizationStrings.Get("UPGRADE_DESCRIPTION_" + upgradeKey.ToUpperInvariant()));
		}

		private void UpdateAdditionalDescription()
		{
			// Native ELF VA 0x13d7320 and predicate 0x13d83d8.
			UpgradeDef definition = upgradesModel.GetDef(upgradeKey);
			bool additional = definition != null && definition.Modifiers.Any(modifier => modifier.StatKey == "map_spawner_timer_reduce");
			infoButton.gameObject.SetActive(additional); if (!additional) descriptionBg.SetActive(false);
		}

		private void ToggleAdditionalDescription()
		{
			// Native ELF VA 0x13d7aa4.
			descriptionBg.SetActive(!descriptionBg.activeSelf);
		}

		private void UpdateCostDisplay(bool isMaxed, bool canAfford)
		{
			// Native ELF VA 0x13d75d4; missing soul cost hides both actual UI objects.
			UpgradeCostInstance? cost = isMaxed ? null : upgradesModel.GetNextSoulCost(upgradeKey);
			if (!cost.HasValue) { costText.gameObject.SetActive(false); soulView.gameObject.SetActive(false); return; }
			soulView.Setup(soulStyleConfig.GetStyle(upgradesModel.GetNextSoulKey(upgradeKey))); soulView.gameObject.SetActive(true);
			string amount = cost.Value.Amount.Format(); costText.text = canAfford ? amount : "<color=red>" + amount + "</color>";
			costText.gameObject.SetActive(true);
		}

		private void UpdateUpgradeButton(bool isMaxed, bool canAfford)
		{
			// Native ELF VA 0x13d7868; native shop query is preserved, no invented purchase availability.
			bool available = upgradesModel.IsAvailable(upgradeKey);
			bool hasOffer = available && !canAfford && HasShopLotsForUpgrade();
			bool interactable = available && !isMaxed && (canAfford || hasOffer);
			upgradeButton.interactable = interactable; upgradeButton.gameObject.SetActive(!isMaxed);
			double experience = isMaxed ? 0.0 : upgradesModel.GetNextUpgradeExperience(upgradeKey);
			experienceText.gameObject.SetActive(experience > 0.0);
			experienceText.text = LocalizationStrings.Get("AMULET_UPGRADE_EXP_REWARD").Replace("{value}",experience.Format());
			experienceText.color = interactable ? ExperienceAvailableColor : ExperienceLockedColor;
		}

		private void HandleUpgradeClick()
		{
			// Native ELF VA 0x13d7d54; graph subscriber owns the actual upgrade command.
			PlayUpgradeButtonAnimation();
			if (upgradesModel.CanAfford(upgradeKey)) m_OnUpgradeClicked?.Invoke(); else OpenMissingCurrencyPurchase();
		}

		private void PlayUpgradeButtonAnimation()
		{
			// Native ELF VA 0x13d7dac.
			upgradeButtonAnimation.ClearPlayingSequence(); upgradeButtonAnimation.ResetToInitialState(); upgradeButtonAnimation.Play();
		}

		private void OpenMissingCurrencyPurchase()
		{
			// Native ELF VA 0x13d7df8; popup user-close callback is distinct from navigator's pool callback.
			var missing = GetFirstMissingCurrency(); if (string.IsNullOrEmpty(missing.Item1)) return;
			UINavigator navigator = GameContext.Get<UINavigator>();
			if (BuyGemsPopupView.IsChaoticSoulCurrency(missing.Item1)) navigator.Open<ShopView>("Assets/Prefabs/UI/Shop.prefab").OpenChaoticSoulOffer();
			else {
			    BuyGemsPopupView popup = navigator.Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab");
			    popup.SetUserCloseCallback(() => m_OnBuyCurrencyPopupClosed?.Invoke()); popup.Setup(missing.Item1,missing.Item2);
			}
		}

		private (string, double) GetFirstMissingCurrency()
		{
			// Native ELF VA 0x13d7fcc; returns the full required amount, not a computed deficit.
			var costs = upgradesModel.GetNextCost(upgradeKey); if (costs == null) return (null,0.0);
			CurrencyModel currency = GameContext.Get<CurrencyModel>();
			foreach (UpgradeCostInstance cost in costs)
			    if (!string.IsNullOrEmpty(cost.CurrencyKey) && !currency.HasEnough(cost.CurrencyKey,cost.Amount)) return (cost.CurrencyKey,cost.Amount);
			return (null,0.0);
		}

		private bool HasShopLotsForUpgrade()
		{
			// Native ELF VA 0x13d7ad8; only first unaffordable nonempty cost determines offer availability.
			var costs = upgradesModel.GetNextCost(upgradeKey); if (costs == null) return false;
			CurrencyModel currency = GameContext.Get<CurrencyModel>();
			foreach (UpgradeCostInstance cost in costs)
			    if (!string.IsNullOrEmpty(cost.CurrencyKey) && !currency.HasEnough(cost.CurrencyKey,cost.Amount))
			        return BuyGemsPopupView.HasPurchaseOptionForCurrency(shopLotRepository,cost.CurrencyKey,cost.Amount);
			return false;
		}

		public override void Cleanup()
		{
			// Native ELF VA 0x13d829c.
			base.Cleanup(); m_OnUpgradeClicked = null; m_OnBuyCurrencyPopupClosed = null; upgradeKey = null;
		}
	}
}
