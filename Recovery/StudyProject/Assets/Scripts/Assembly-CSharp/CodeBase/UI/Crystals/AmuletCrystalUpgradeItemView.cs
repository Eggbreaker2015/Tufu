using System.Text;
using DG.Tweening;
using Codebase.Analytics.Events;
using Codebase.Utilities;
using Codebase.UI.Shop;
using AssetKits.ParticleImage;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Economy;
using Codebase.Meta.Upgrades;
using Codebase.Shop;
using Codebase.UI.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalUpgradeItemView : View
	{
		private const string MaxLevelColorHex = "FFF04A";

		private const string NextValueColorHex = "42FF8E";

		private const string ValueArrowSprite = "<sprite name=atlas_59>";

		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text levelText;

		[SerializeField]
		private TMP_Text currentValueText;

		[SerializeField]
		private Button upgradeButton;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private Button infoButton;

		[Header("Maxed")]
		[SerializeField]
		private Image maxedImage;

		[SerializeField]
		private ImageOverlay imageOverlay;

		[Header("Highlight")]
		[SerializeField]
		private Image highlightImage;

		[SerializeField]
		private float highlightPeakAlpha = 0.25f; // Original ctor ELF 0x13b2bcc.

		[Header("Effects")]
		[SerializeField]
		private ParticleImage upgradeParticle;

		[SerializeField]
		private AnimationSequencerController upgradeAnimation;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private UpgradeStyleConfig upgradeStyleConfig;

		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private IAnalyticsWrapper analytics;

		private static readonly StringBuilder CostBuffer = new StringBuilder(); // Original cctor 0x13b2bdc.

		private static readonly StringBuilder ValueBuffer = new StringBuilder();

		private UpgradeDef def;

		private int lastRenderedLevel;

		public string UpgradeKey => def?.Key; // Original ELF 0x13b1524.

		private UpgradeModel UpgradesModel => crystalModel.Upgrades; // Original ELF 0x13b153c.

		protected override void Awake()
		{
			// Original ELF 0x13b1554.
			base.Awake();
			upgradeButton.onClick.AddListener(HandleUpgrade);
			infoButton.onClick.AddListener(HandleInfo);
		}

		public void Setup(UpgradeDef def)
		{
			// Original ELF 0x13b1648. Initialize schedules the original View reaction.
			this.def = def;
			lastRenderedLevel = UpgradesModel == null ? 0 : UpgradesModel.GetLevel(def.Key);
			Initialize();
		}

		public void PlayHighlight(float duration = 1.4f)
		{
			// Original ELF 0x13b16b4; initial RGB is retained while alpha is cleared.
			DOTween.Kill(highlightImage);
			Color color = highlightImage.color;
			color.a = 0f;
			highlightImage.color = color;
			DOTween.Sequence().SetTarget(highlightImage)
				.Append(highlightImage.DOFade(highlightPeakAlpha, duration * 0.5f))
				.Append(highlightImage.DOFade(0f, duration * 0.5f));
		}

		protected override void Render()
		{
			// Original ELF 0x13b17e0.
			if (def == null || UpgradesModel == null) return;
			int level = UpgradesModel.GetLevel(def.Key);
			bool maxed = UpgradesModel.IsMaxLevel(def.Key);
			Sprite sprite = upgradeStyleConfig.GetIcon(def.Key);
			icon.sprite = sprite;
			icon.enabled = sprite != null;
			nameText.text = LocalizationStrings.Get("UPGRADE_" + def.Key);
			levelText.text = maxed
				? "<color=#FFF04A>" + LocalizationStrings.Get("UPGRADE_MAX").ToUpper() + "</color>"
				: LocalizationStrings.Get("UPGRADE_LEVEL").Replace("{current}", level.ToString()).Replace("{max}", def.MaxLevel.ToString());
			currentValueText.text = BuildValueText(maxed);
			costText.text = maxed ? LocalizationStrings.Get("UPGRADE_MAX") : BuildCostText();
			upgradeButton.gameObject.SetActive(!maxed);
			upgradeButton.interactable = !maxed && (UpgradesModel.CanAfford(def.Key) || HasShopLotsForUpgrade());
			maxedImage.gameObject.SetActive(maxed);
			imageOverlay.enabled = maxed;
			infoButton.gameObject.SetActive(def.Key == "crystal_blacksmithing" || def.Key == "crystal_enchantment");
			if (lastRenderedLevel < level) PlayUpgradeEffects();
			lastRenderedLevel = level;
		}

		private string BuildValueText(bool isMaxLevel)
		{
			// Original ELF 0x13b1c48.
			ValueBuffer.Clear();
			string current = def.FormatDisplayValue(UpgradesModel.GetTotalModifierValue(def.Key, 0));
			if (isMaxLevel)
				ValueBuffer.Append("<color=#").Append(MaxLevelColorHex).Append('>').Append(current).Append("</color>");
			else
			{
				string next = def.FormatDisplayValue(UpgradesModel.GetNextTotalModifierValue(def.Key, 0));
				ValueBuffer.Append(current).Append(ValueArrowSprite).Append(" <color=#")
					.Append(NextValueColorHex).Append('>').Append(next).Append("</color>");
			}
			return ValueBuffer.ToString();
		}

		private string BuildCostText()
		{
			// Original ELF 0x13b1ebc.
			var costs = UpgradesModel.GetNextCost(def.Key);
			if (costs == null || costs.Count == 0) return string.Empty;
			CostBuffer.Clear();
			for (int i = 0; i < costs.Count; i++)
			{
				if (i != 0) CostBuffer.Append("<br>");
				Sprite sprite = currencyStyleConfig.GetIcon(costs[i].CurrencyKey);
				if (sprite != null) CostBuffer.Append("<sprite name=").Append(sprite.name).Append("> ");
				CostBuffer.Append(costs[i].Amount.Format());
			}
			return CostBuffer.ToString();
		}

		private void HandleUpgrade()
		{
			// Original ELF 0x13b2574. Real local cost/model mutation precedes the event.
			if (def == null) return;
			if (!UpgradesModel.CanAfford(def.Key)) { OpenBuyCurrencyPopup(); return; }
			if (UpgradesModel.TryUpgrade(def.Key)) analytics.Send(new CrystalsUpgradeEvent(def.Key));
		}

		private void OpenBuyCurrencyPopup()
		{
			// Original ELF 0x13b26d8; required total is passed to the popup, which computes its deficit.
			var missing = GetFirstMissingCurrency();
			if (string.IsNullOrEmpty(missing.Item1)) return;
			GameContext.Get<UINavigator>().Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab").Setup(missing.Item1, missing.Item2);
		}

		private (string, double) GetFirstMissingCurrency()
		{
			// Original ELF 0x13b27bc.
			var costs = UpgradesModel.GetNextCost(def.Key);
			if (costs == null) return (null, 0d);
			var currency = GameContext.Get<CurrencyModel>();
			foreach (var cost in costs)
				if (!string.IsNullOrEmpty(cost.CurrencyKey) && !currency.HasEnough(cost.CurrencyKey, cost.Amount))
					return (cost.CurrencyKey, cost.Amount);
			return (null, 0d);
		}

		private bool HasShopLotsForUpgrade()
		{
			// Original ELF 0x13b21c4: only the first deficient currency is examined, with default requiredAmount=0.
			var costs = UpgradesModel.GetNextCost(def.Key);
			if (costs == null) return false;
			var currency = GameContext.Get<CurrencyModel>();
			foreach (var cost in costs)
				if (!string.IsNullOrEmpty(cost.CurrencyKey) && !currency.HasEnough(cost.CurrencyKey, cost.Amount))
					return BuyGemsPopupView.GetLotsForCurrency(shopLotRepository, cost.CurrencyKey).Count > 0;
			return false;
		}

		private void PlayUpgradeEffects()
		{
			// Original ELF 0x13b2468; ParticleImage emission remains a separately documented plugin limitation.
			upgradeParticle.Play();
			upgradeAnimation.ClearPlayingSequence();
			upgradeAnimation.ResetToInitialState();
			upgradeAnimation.Play();
			audioPlayer.PlaySoundOnce("crystal_upgrade");
		}

		private void HandleInfo()
		{
			// Original ELF 0x13b2a9c.
			if (def == null) return;
			CrystalRarityChancesMode mode;
			if (def.Key == "crystal_blacksmithing") mode = CrystalRarityChancesMode.Blacksmithing;
			else if (def.Key == "crystal_enchantment") mode = CrystalRarityChancesMode.Enchantment;
			else return;
			GameContext.Get<UINavigator>().Open<AmuletCrystalRarityChancesView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalRarityChances.prefab", true).Setup(mode);
		}
	}
}
