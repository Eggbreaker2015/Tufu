using Codebase.UI.VIP;
using Codebase.UI.Shop;
using Codebase.UI.Amulet;
using Codebase.VIP.Features;
using System.Linq;
using BrunoMikoski.AnimationSequencer;
using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Utilities;
using Codebase.VIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalSlotView : View
	{
		[SerializeField]
		[KeysDropdown(typeof(CrystalSlotKeys))]
		private string slotKey;

		[SerializeField]
		private Image glow;

		[SerializeField]
		private Image crystalIcon;

		[SerializeField]
		private Image enchantmentIcon;

		[SerializeField]
		private TMP_Text levelText;

		[SerializeField]
		private Button button;

		[Header("Locked State")]
		[SerializeField]
		private GameObject lockedState;

		[SerializeField]
		private GameObject lockedVip;

		[SerializeField]
		private TMP_Text lockedVipText;

		[SerializeField]
		private GameObject lockedUpgrade;

		[SerializeField]
		private TMP_Text lockedUpgradeText;

		[SerializeField]
		private Button lockedUpgradeMagnifierButton;

		[Header("Requires Amulet Level")]
		[SerializeField]
		private TMP_Text requiresAmuletLevelText;

		[SerializeField]
		private AnimationSequencerController requiresAmuletLevelAnimation;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private CrystalStyleConfig styleConfig;

		[Inject]
		private ABTestService abTestService;

		[Inject]
		private VipConfig vipConfig;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private AmuletUpgradesModel amuletUpgradesModel;

		[Inject]
		private AmuletUpgradeConfig amuletUpgradeConfig;

		[Inject]
		private AmuletModel amuletModel;

		private const float GlowFlickerMinAlpha = 0.5f;

		private const float GlowFlickerMaxAlpha = 1f;

		private const float GlowFlickerSpeedA = 2.5f;

		private const float GlowFlickerSpeedB = 5.2f;

		private float glowFlickerOffset;

		public string SlotKey => this.slotKey;

		public CrystalItemModel EquippedCrystal => crystalModel.Crystals.FirstOrDefault(c => c.EquippedSlotKey == slotKey); // Native ELF0x13a9f7c / closure13ab2f8.

		private CrystalSlotModel SlotModel => crystalModel.GetSlot(slotKey); // Native ELF0x13aa03c.

		protected override void Awake()
		{
            // Native ELF0x13aa05c.
            base.Awake(); button.onClick.AddListener(HandleClick); lockedUpgradeMagnifierButton.onClick.AddListener(HandleLockedMagnifierClick);
            glowFlickerOffset = Random.Range(0f, 100f);
        }

		private void LateUpdate()
		{
            // Native ELF0x13aa16c.
            if (!glow.gameObject.activeInHierarchy) return;
            float time = Time.time;
            float a = Mathf.PerlinNoise(time * 2.5f + glowFlickerOffset, 0f);
            float b = Mathf.PerlinNoise(0f, time * 5.2f + glowFlickerOffset);
            float value = Mathf.Max(0f, Mathf.Clamp01(a * .65f + b * .55f));
            var color = glow.color; color.a = value * .5f + .5f; glow.color = color;
        }

		protected override void Render()
		{
            // Native ELF0x13aa274.
            var slot = SlotModel;
            if (slot == null || slot.IsUnlocked) { lockedState.SetActive(false); RenderUnlocked(); }
            else { lockedState.SetActive(true); RenderLocked(slot); }
        }

		private void RenderLocked(CrystalSlotModel slot)
		{
            // Native ELF0x13aa2e8.
            glow.gameObject.SetActive(false); crystalIcon.gameObject.SetActive(false); enchantmentIcon.gameObject.SetActive(false); levelText.gameObject.SetActive(false);
            bool vip = slot.UnlockSource == CrystalSlotUnlockSource.Vip; lockedVip.SetActive(vip); lockedUpgrade.SetActive(!vip);
            if (vip) { lockedVipText.text = string.Format("VIP {0}", GetRequiredVipLevel(slot.Key)); return; }
            requiresAmuletLevelText.gameObject.SetActive(false);
            lockedUpgradeText.text = LocalizationStrings.Get(slot.UnlockSource == CrystalSlotUnlockSource.CrystalPass ? "CRYSTALS_SLOT_LOCKED_CRYSTAL_PASS" : slot.UnlockSource == CrystalSlotUnlockSource.CrystalUpgrades ? "CRYSTALS_SLOT_LOCKED_CRYSTAL_UPGRADES" : "CRYSTALS_SLOT_LOCKED_SOUL_TREE");
            lockedUpgradeMagnifierButton.gameObject.SetActive(true);
        }

		private int GetRequiredVipLevel(string slotKey)
		{
            // Native ELF0x13aa848.
            foreach (var feature in vipConfig.GetFeatureConfigsByKey("crystal_slots"))
                if (feature is VipCrystalSlotsConfig config && config.SlotKey == slotKey) return config.UnlockLevel;
            return 0;
        }

		private void RenderUnlocked()
		{
            // Native ELF0x13aa51c.
            var crystal = EquippedCrystal;
            if (crystal == null) { glow.gameObject.SetActive(false); crystalIcon.gameObject.SetActive(false); enchantmentIcon.gameObject.SetActive(false); levelText.gameObject.SetActive(false); return; }
            glow.gameObject.SetActive(true); glow.color = styleConfig.GetRarityColor(crystal.Rarity);
            var icon = styleConfig.GetCrystalSprite(crystal.BaseStatType, crystal.EnchantLevel); crystalIcon.gameObject.SetActive(icon != null); crystalIcon.sprite = icon;
            var enchant = styleConfig.GetEnchantmentIcon(crystal.EnchantLevel); enchantmentIcon.gameObject.SetActive(crystal.EnchantLevel > 0 && enchant != null); enchantmentIcon.sprite = enchant; enchantmentIcon.color = styleConfig.GetRarityColor(crystal.Rarity);
            levelText.gameObject.SetActive(true); levelText.text = crystal.Level.ToString();
        }

		private void HandleClick()
		{
            // Native ELF0x13aaa78.
            var slot = SlotModel;
            if (slot != null && !slot.IsUnlocked) { HandleLockedClick(slot); return; }
            var crystal = EquippedCrystal; if (crystal == null) return;
            navigator.Open<AmuletCrystalContextView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalContextView.prefab", true).Setup(crystal);
        }

		private void HandleLockedMagnifierClick()
		{
            // Native ELF0x13aabc8.
            var slot = SlotModel; if (slot != null && !slot.IsUnlocked) HandleLockedClick(slot);
        }

		private void HandleLockedClick(CrystalSlotModel slot)
		{
            // Native ELF0x13aab40.
            switch (slot.UnlockSource) {
                case CrystalSlotUnlockSource.Graph: HandleGraphUnlockClick(slot.UnlockUpgradeKey); break;
                case CrystalSlotUnlockSource.CrystalPass: OpenCrystalPassShop(); break;
                case CrystalSlotUnlockSource.Vip: OpenVipLevel(GetRequiredVipLevel(slot.Key)); break;
                case CrystalSlotUnlockSource.CrystalUpgrades: OpenCrystalUpgrades(); break;
            }
        }

		private void HandleGraphUnlockClick(string upgradeKey)
		{
            // Native ELF0x13aac0c.
            if (!string.IsNullOrEmpty(upgradeKey)) {
                if (amuletUpgradesModel.IsAvailable(upgradeKey)) { OpenGraphUpgrade(upgradeKey); return; }
                ShowRequiresAmuletLevel(amuletUpgradeConfig.GetUpgrade(upgradeKey).RequiredAmuletLevel);
            } else ShowRequiresAmuletLevel(unchecked(amuletModel.MaxLevel + 1));
        }

		private void ShowRequiresAmuletLevel(int requiredLevel)
		{
            // Native ELF0x13aae38.
            requiresAmuletLevelText.gameObject.SetActive(true); requiresAmuletLevelText.text = LocalizationStrings.Get("CRYSTALS_SLOT_REQUIRES_AMULET_LEVEL").Replace("{value}", requiredLevel.ToString());
            requiresAmuletLevelAnimation.ClearPlayingSequence(); requiresAmuletLevelAnimation.ResetToInitialState(); requiresAmuletLevelAnimation.Play();
        }

		private void OpenGraphUpgrade(string upgradeKey)
		{
            // Native ELF0x13aaf4c.
            navigator.Open<AmuletUpgradeGraphView>("Assets/Prefabs/UI/Amulet/AmuletUpgradesGraph.prefab").FocusOnUpgrade(upgradeKey);
        }

		private void OpenVipLevel(int level)
		{
            // Native ELF0x13aaca8.
            navigator.Open<VipView>("Assets/Prefabs/UI/VIP/VIP.prefab").SetPreviewLevel(level);
        }

		private void OpenCrystalPassShop()
		{
            // Native ELF0x13aad28.
            if (!abTestService.IsCrystalPassEnabled()) return;
            navigator.Open<ShopView>("Assets/Prefabs/UI/Shop.prefab").OpenAmuletTab();
        }

		private void OpenCrystalUpgrades()
		{
            // Native ELF0x13aadc0.
            navigator.Open<AmuletCrystalUpgradesView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalUpgradesView.prefab", true).HighlightSlotUnlock(slotKey);
        }
	}
}
