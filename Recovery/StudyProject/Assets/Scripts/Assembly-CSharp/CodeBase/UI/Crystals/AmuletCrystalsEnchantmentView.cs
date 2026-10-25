using Codebase.Crystals.Commands;
using Codebase.UI.Shop;
using Codebase.Utilities;
using Codebase.Unit;
using System;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalsEnchantmentView : PopupView
	{
		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button enchantButton;

		[SerializeField]
		private Button guaranteeEnchantButton;

		[SerializeField]
		private TMP_Text guaranteeProgressText;

		[SerializeField]
		private AmuletCrystalItemView currentItemView;

		[SerializeField]
		private AmuletCrystalItemView nextItemView;

		[SerializeField]
		private TMP_Text currentBaseStatNameText;

		[SerializeField]
		private TMP_Text currentBaseStatValueText;

		[SerializeField]
		private TMP_Text currentSpecialStatNameText;

		[SerializeField]
		private TMP_Text currentSpecialStatValueText;

		[SerializeField]
		private TMP_Text currentLegendaryAbilityNameText;

		[SerializeField]
		private TMP_Text currentLegendaryAbilityValueText;

		[SerializeField]
		private Image currentBaseStatIcon;

		[SerializeField]
		private Image currentSpecialStatIcon;

		[SerializeField]
		private Image currentLegendaryStatIcon;

		[SerializeField]
		private TMP_Text nextBaseStatNameText;

		[SerializeField]
		private TMP_Text nextBaseStatValueText;

		[SerializeField]
		private TMP_Text nextSpecialStatNameText;

		[SerializeField]
		private TMP_Text nextSpecialStatValueText;

		[SerializeField]
		private TMP_Text nextLegendaryAbilityNameText;

		[SerializeField]
		private TMP_Text nextLegendaryAbilityValueText;

		[SerializeField]
		private Image nextBaseStatIcon;

		[SerializeField]
		private Image nextSpecialStatIcon;

		[SerializeField]
		private Image nextLegendaryStatIcon;

		[SerializeField]
		private Sprite specialStatPlaceholderIcon;

		[SerializeField]
		private TMP_Text currentSellPriceText;

		[SerializeField]
		private TMP_Text nextSellPriceText;

		[SerializeField]
		private TMP_Text currentValueText;

		[SerializeField]
		private TMP_Text nextValueText;

		[SerializeField]
		private TMP_Text successChanceText;

		[SerializeField]
		private TMP_Text enchantStonesCountTxt;

		[SerializeField]
		private TMP_Text enchantStonesResetTimeTxt;

		[SerializeField]
		private CrystalEnchantLifesView lifesView;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private PlayerModel playerModel;

		private CrystalItemModel model;

		protected override void Awake()
		{
            // Native ELF0x13a7ec4.
            base.Awake(); closeButton.onClick.AddListener(HandleClose); infoButton.onClick.AddListener(OpenTutorial);
            enchantButton.onClick.AddListener(HandleEnchant); guaranteeEnchantButton.onClick.AddListener(HandleGuaranteeEnchant);
        }

		private void HandleClose()
		{
            // Native ELF0x13a8058.
            audioPlayer.PlaySoundOnce("tap"); Close();
        }

		public void Setup(CrystalItemModel model)
		{
            // Native ELF0x13a8124.
            this.model = model; Injector.InjectInto(currentItemView); Injector.InjectInto(nextItemView);
            currentItemView.Setup(model); nextItemView.SetupPreview(model, crystalModel.GetNextEnchantLevel(model));
            Initialize(); Render(); TryShowTutorial();
        }

		private void TryShowTutorial()
		{
            // Native ELF0x13a8218.
            if (!playerModel.Data.SeenAmuletCrystalsEnchantment.Value) OpenTutorial();
        }

		private void OpenTutorial()
		{
            // Native ELF0x13a829c.
            navigator.Open<AmuletCrystalsEnchantmentTutorialView>("Assets/Prefabs/Ui/Amulet/Crystals/AmuletCrystalsEnchantmentTutorial.prefab", true);
        }

		public override void Cleanup()
		{
            // Native ELF0x13a8308.
            base.Cleanup(); model = null;
        }

		protected override void Render()
		{
            // Native ELF0x13a832c.
            double enchantment = crystalModel.Enchantment.Value;
            int level = double.IsNaN(enchantment) ? 0 : double.IsPositiveInfinity(enchantment) ? int.MinValue : enchantment >= int.MaxValue ? int.MaxValue : enchantment <= int.MinValue ? int.MinValue : (int)enchantment;
            titleText.text = LocalizationStrings.Get("CRYSTALS_ENCHANTMENT_TITLE_LEVEL").Replace("{value}", level.ToString());
            if (model == null) return;
            RenderStats(false, currentBaseStatNameText, currentBaseStatValueText, currentBaseStatIcon, currentSpecialStatNameText, currentSpecialStatValueText, currentSpecialStatIcon, currentLegendaryAbilityNameText, currentLegendaryAbilityValueText, currentLegendaryStatIcon);
            RenderStats(true, nextBaseStatNameText, nextBaseStatValueText, nextBaseStatIcon, nextSpecialStatNameText, nextSpecialStatValueText, nextSpecialStatIcon, nextLegendaryAbilityNameText, nextLegendaryAbilityValueText, nextLegendaryStatIcon);
            currentSellPriceText.text = CrystalStatTextFormatter.FormatSellPrice(crystalModel.CalculateSellPrice(model));
            nextSellPriceText.text = CrystalStatTextFormatter.FormatSellPrice(crystalModel.CalculatePreviewSellPrice(model));
            double level0 = GetLevel0BaseStatValue(), preview = crystalModel.CalculatePreviewBaseStatValue(model);
            currentValueText.text = FormatCrystalValuePercent(model.BaseStatValue, level0, false); nextValueText.text = FormatCrystalValuePercent(preview, level0, true);
            successChanceText.text = CrystalStatTextFormatter.FormatSuccessChance(crystalModel.GetEnchantSuccessChance(model));
            enchantButton.interactable = !crystalModel.IsAtMaxEnchantLevel(model); RenderExtraAttempts(); RenderGuaranteeEnchant();
            enchantStonesCountTxt.text = string.Format("{0}<size=80%>/{1}", crystalModel.EnchantmentStonesCount, crystalModel.MaxEnchantmentStones); UpdateEnchantStonesTime();
        }

		private void UpdateEnchantStonesTime()
		{
            // Native ELF0x13a9088.
            enchantStonesResetTimeTxt.text = TimeLocalizer.GetFormatTime((float)crystalModel.TimeToNextEnchantStoneReset.TotalSeconds, true);
        }

		private void RenderExtraAttempts()
		{
            // Native ELF0x13a8e1c.
            int current = model.EnchantLevel, next = crystalModel.GetNextEnchantLevel(model);
            int extra = crystalModel.IsAtMaxEnchantLevel(model) ? 0 : crystalModel.GetEnchantExtraAttempts(current); lifesView.Setup(next, extra, extra);
        }

		private void RenderGuaranteeEnchant()
		{
            // Native ELF0x13a8eb4.
            GameObject progressRoot = guaranteeProgressText.transform.parent.gameObject; bool unlocked = crystalModel.IsGuaranteeEnchantUnlocked;
            guaranteeEnchantButton.gameObject.SetActive(unlocked); if (!unlocked) { progressRoot.SetActive(false); return; }
            bool ready = crystalModel.IsGuaranteeEnchantReady; guaranteeEnchantButton.interactable = ready && !crystalModel.IsAtMaxEnchantLevel(model); progressRoot.SetActive(true);
            guaranteeProgressText.text = ready ? CrystalStatTextFormatter.WrapColor(LocalizationStrings.Get("CRYSTALS_GUARANTEED_ENCHANT"), "#FFD500") : CrystalStatTextFormatter.FormatGuaranteeEnchantProgress(Math.Max(1, unchecked(crystalModel.GuaranteeEnchantThreshold - crystalModel.GuaranteeEnchantProgress)));
        }

		private void RenderStats(bool useNextLevel, TMP_Text baseNameText, TMP_Text baseValueText, Image baseIcon, TMP_Text specialNameText, TMP_Text specialValueText, Image specialIcon, TMP_Text legendaryNameText, TMP_Text legendaryValueText, Image legendaryIcon)
		{
            // Native ELF0x13a86a8.
            string nextColor = useNextLevel ? "#90E810" : null;
            double baseValue = useNextLevel ? crystalModel.CalculatePreviewBaseStatValue(model) : model.BaseStatValue;
            ApplyStatRow(baseNameText, baseValueText, CrystalStatTextFormatter.FormatStatNameValue(iconStyleConfig, model.BaseStatType, baseValue, false, nextColor, false), "white");
            SetStatIcon(baseIcon, CrystalStatTextFormatter.GetStatIconSprite(iconStyleConfig, model.BaseStatType));
            CrystalStatTextFormatter.CrystalStatNameValue special = default; Sprite specialSprite = null;
            if (model.HasSpecialStat) {
                int level = useNextLevel ? crystalModel.GetNextEnchantLevel(model) : model.EnchantLevel;
                float value = crystalModel.GetSpecialStatValue(model, model.SpecialStatType, level);
                special = CrystalStatTextFormatter.FormatStatNameValue(iconStyleConfig, model.SpecialStatType, value, true, nextColor, false);
                specialSprite = CrystalStatTextFormatter.GetStatIconSprite(iconStyleConfig, model.SpecialStatType);
            } else if (useNextLevel && !crystalModel.IsAtMaxEnchantLevel(model)) {
                special = new CrystalStatTextFormatter.CrystalStatNameValue { Name = LocalizationStrings.Get("CRYSTALS_SPECIAL_STAT"), Value = string.Empty }; specialSprite = specialStatPlaceholderIcon;
            }
            ApplyStatRow(specialNameText, specialValueText, special, "#FFD500"); SetStatIcon(specialIcon, specialSprite);
            CrystalStatTextFormatter.CrystalStatNameValue last = default; Sprite lastSprite = null; string lastColor = "#FFD500";
            if (model.HasSecondaryStat) {
                double value = useNextLevel ? crystalModel.CalculatePreviewSecondaryStatValue(model) : model.SecondaryStatValue;
                last = CrystalStatTextFormatter.FormatStatNameValue(iconStyleConfig, model.SecondaryStatType, value, false, nextColor, false);
                lastSprite = CrystalStatTextFormatter.GetStatIconSprite(iconStyleConfig, model.SecondaryStatType); lastColor = "white";
            } else if (model.HasLegendaryAbility) {
                int? previewLevel = useNextLevel ? (int?)crystalModel.GetNextEnchantLevel(model) : null;
                last = CrystalStatTextFormatter.FormatLegendaryAbilityNameValue(crystalModel, model.LegendaryAbility, nextColor, model, previewLevel, false);
                lastSprite = crystalModel.Config.GetLegendaryAbilityIcon(model.LegendaryAbility);
                if (string.IsNullOrEmpty(last.Name)) { last = default; lastSprite = null; }
            }
            ApplyStatRow(legendaryNameText, legendaryValueText, last, lastColor); SetStatIcon(legendaryIcon, lastSprite);
        }

		private static void SetStatIcon(Image image, Sprite sprite)
		{
            // Native ELF0x13a97e8.
            image.sprite = sprite; image.enabled = sprite != null;
        }

		private static void ApplyStatRow(TMP_Text nameText, TMP_Text valueText, CrystalStatTextFormatter.CrystalStatNameValue parts, string color)
		{
            // Native ELF0x13a95e8.
            bool name = !string.IsNullOrEmpty(parts.Name), value = name && !string.IsNullOrEmpty(parts.Value);
            nameText.gameObject.SetActive(name); valueText.gameObject.SetActive(value);
            if (name) nameText.text = CrystalStatTextFormatter.WrapColor(parts.Name, color);
            if (value) valueText.text = CrystalStatTextFormatter.WrapColor(parts.Value, color);
        }

		private void HandleEnchant()
		{
            // Native ELF0x13a9b90.
            if (model == null || crystalModel.IsAtMaxEnchantLevel(model)) return;
            if (crystalModel.EnchantmentStonesCount <= 0.0) { OpenBuyStonesPopup(); return; }
            CrystalItemModel crystal = model;
            EnchantAttempt attempt = App.ExecuteCommand<EnchantCrystalCommand, EnchantAttempt>(new EnchantCrystalCommand { Crystal = crystal, Guaranteed = false });
            if (!attempt.Performed) return; Close();
            navigator.Open<AmuletCrystalEnchantmentResultView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalEnchantmentResult.prefab", true).Setup(crystal, attempt, false, false);
        }

		private void HandleGuaranteeEnchant()
		{
            // Native ELF0x13a9dbc.
            if (model == null || crystalModel.IsAtMaxEnchantLevel(model)) return;
            if (!crystalModel.IsGuaranteeEnchantReady) return;
            if (crystalModel.EnchantmentStonesCount <= 0.0) { OpenBuyStonesPopup(); return; }
            CrystalItemModel crystal = model;
            EnchantAttempt attempt = App.ExecuteCommand<EnchantCrystalCommand, EnchantAttempt>(new EnchantCrystalCommand { Crystal = crystal, Guaranteed = true });
            if (!attempt.Performed) return; Close();
            navigator.Open<AmuletCrystalEnchantmentResultView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalEnchantmentResult.prefab", true).Setup(crystal, attempt, false, true);
        }

		private void OpenBuyStonesPopup()
		{
            // Native ELF0x13a9d28.
            navigator.Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab").Setup("crystal.enchantment_stones", 1.0);
        }

		private double GetLevel0BaseStatValue()
		{
            // Native ELF0x13a8c00.
            return crystalModel.CalculateBaseStatValue(model.Level, model.Rarity, model.BaseStatType);
        }

		private string FormatCrystalValuePercent(double currentBaseValue, double level0Value, bool useNextLevel)
		{
            // Native ELF0x13a8c78.
            double value = level0Value > 0.0 ? currentBaseValue / level0Value * 100.0 : 0.0;
            string text = string.Format("{0:F0}%", value); return useNextLevel ? CrystalStatTextFormatter.WrapColor(text, "#90E810") : text;
        }
	}
}
