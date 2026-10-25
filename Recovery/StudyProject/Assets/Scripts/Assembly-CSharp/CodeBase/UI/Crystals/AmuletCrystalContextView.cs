using Codebase.UI.Amulet;
using Codebase.Crystals.Commands;
using Codebase.Utilities;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalContextView : PopupView
	{
		[SerializeField]
		private AmuletCrystalItemView itemView;

		[SerializeField]
		private TMP_Text rarityText;

		[SerializeField]
		private TMP_Text baseStatsText;

		[SerializeField]
		private TMP_Text specialStatText;

		[SerializeField]
		private TMP_Text legendaryAbilityText;

		[SerializeField]
		private Button sellButton;

		[SerializeField]
		private TMP_Text sellPriceText;

		[SerializeField]
		private Button enchantButton;

		[SerializeField]
		private GameObject enchantActiveState;

		[SerializeField]
		private GameObject enchantMaxedState;

		[SerializeField]
		private Image enchantMaxedStateImage;

		[SerializeField]
		private TMP_Text enchantChanceText;

		[Header("Enchant Locked State")]
		[SerializeField]
		private GameObject enchantLockedState;

		[SerializeField]
		private TMP_Text enchantRequiresAmuletLevelText;

		[SerializeField]
		private Button equipButton;

		[SerializeField]
		private TMP_Text equipButtonText;

		[SerializeField]
		private Button closeButton;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private CrystalStyleConfig crystalStyleConfig;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private AmuletUpgradesModel amuletUpgradesModel;

		[Inject]
		private AmuletUpgradeConfig amuletUpgradeConfig;

		[Inject]
		private AmuletModel amuletModel;

		private CrystalItemModel model;

		protected override void Awake()
		{
            // Native ELF0x1673488.
            base.Awake(); sellButton.onClick.AddListener(HandleSell); enchantButton.onClick.AddListener(HandleEnchant);
            equipButton.onClick.AddListener(HandleEquip); closeButton.onClick.AddListener(Close);
        }

		public void Setup(CrystalItemModel model)
		{
            // Native ELF0x167361c.
            this.model = model; Injector.InjectInto(itemView); itemView.Setup(model); Render();
        }

		public override void Cleanup()
		{
            // Native ELF0x167371c.
            base.Cleanup(); model = null;
        }

		protected override void Render()
		{
            // Native ELF0x1673740.
            if (model == null) return;
            rarityText.text = LocalizationStrings.Get(string.Format("CRYSTALS_RARITY_{0}", model.Rarity));
            rarityText.color = crystalStyleConfig.GetRarityColor(model.Rarity);
            var parts = CrystalStatTextFormatter.BuildCrystalStatsParts(crystalModel, iconStyleConfig, model, true, true);
            if (model.EnchantLevel == 0) parts.Special = CrystalStatTextFormatter.BuildEnchantToUnlockText(crystalStyleConfig, model.Rarity);
            CrystalStatTextFormatter.ApplyStatsParts(parts, baseStatsText, specialStatText, legendaryAbilityText);
            bool sellable = !model.IsEquipped && !model.IsProtected;
            sellButton.gameObject.SetActive(sellable);
            if (sellable) sellPriceText.text = CrystalStatTextFormatter.FormatSellPrice(crystalModel.CalculateSellPrice(model));
            bool unlocked = crystalModel.IsEnchantUnlocked(model), max = crystalModel.IsAtMaxEnchantLevel(model);
            enchantLockedState.SetActive(!unlocked); enchantActiveState.SetActive(unlocked && !max); enchantMaxedState.SetActive(unlocked && max);
            enchantButton.interactable = !(unlocked && max);
            if (!unlocked) enchantRequiresAmuletLevelText.text = LocalizationStrings.Get("CRYSTALS_SLOT_REQUIRES_AMULET_LEVEL").Replace("{value}", GetEnchantUnlockRequiredLevel().ToString());
            else if (!max) enchantChanceText.text = LocalizationStrings.Get("CRYSTALS_ENCHANT_CHANCE").Replace("{value}", CrystalStatTextFormatter.FormatChancePercent(crystalModel.GetEnchantSuccessChance(model)));
            else enchantMaxedStateImage.color = crystalStyleConfig.GetRarityColor(model.Rarity);
            equipButton.gameObject.SetActive(true);
            equipButtonText.text = LocalizationStrings.Get(model.IsEquipped ? "INVENTORY_UNEQUIP" : "INVENTORY_EQUIP");
        }

		private void HandleSell()
		{
            // Native ELF0x1673c58.
            if (model == null) return;
            Vector3 position = sellButton.transform.position;
            App.ExecuteCommand(new SellCrystalCommand { Crystal = model });
            GameContext.Get<AmuletCrystalsView>().PlayCrystalSellDustEffect(position); Close();
        }

		private void HandleEnchant()
		{
            // Native ELF0x1673d9c.
            if (model == null) return;
            if (!crystalModel.IsEnchantUnlocked(model)) { HandleEnchantLockedClick(); return; }
            if (crystalModel.IsAtMaxEnchantLevel(model)) return;
            audioPlayer.PlaySoundOnce("tap");
            navigator.Open<AmuletCrystalsEnchantmentView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalsEnchantmentView.prefab", true).Setup(model);
        }

		private void HandleEnchantLockedClick()
		{
            // Native ELF0x1673efc.
            string key = GetEnchantUnlockUpgradeKey();
            if (!string.IsNullOrEmpty(key) && amuletUpgradesModel.IsAvailable(key)) OpenGraphUpgrade(key);
        }

		private string GetEnchantUnlockUpgradeKey()
		{
            // Native ELF0x1673f58.
            return amuletUpgradesModel.FindUpgradeKeyByModifier("crystal_enchantment_unlock", CrystalRarityStatParam.From(model.Rarity));
        }

		private int GetEnchantUnlockRequiredLevel()
		{
            // Native ELF0x1673bf4.
            string key = GetEnchantUnlockUpgradeKey();
            return string.IsNullOrEmpty(key) ? unchecked(amuletModel.MaxLevel + 1) : amuletUpgradeConfig.GetUpgrade(key).RequiredAmuletLevel;
        }

		private void OpenGraphUpgrade(string upgradeKey)
		{
            // Native ELF0x1673fcc.
            navigator.Open<AmuletUpgradeGraphView>("Assets/Prefabs/UI/Amulet/AmuletUpgradesGraph.prefab").FocusOnUpgrade(upgradeKey);
        }

		private void HandleEquip()
		{
            // Native ELF0x167404c.
            if (model == null) return;
            if (model.IsEquipped) {
                audioPlayer.PlaySoundOnce("tap"); App.ExecuteCommand(new UnequipCrystalCommand { Crystal = model }); Close(); return;
            }
            foreach (var slot in crystalModel.Slots) {
                if (!slot.IsUnlocked || crystalModel.GetEquippedCrystal(slot.Key) != null) continue;
                App.ExecuteCommand(new EquipCrystalCommand { Crystal = model, SlotKey = slot.Key }); Close(); break;
            }
        }
	}
}
