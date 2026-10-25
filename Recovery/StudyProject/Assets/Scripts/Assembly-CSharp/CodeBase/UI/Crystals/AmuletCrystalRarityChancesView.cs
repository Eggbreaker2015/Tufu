using Codebase.Utilities;
using UniMob;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalRarityChancesView : PopupView
	{
		private const string LevelLocKey = "REWARD_LEVEL";

		private const string BlacksmithingTipLocKey = "CRYSTALS_BLACKSMITH_SHORT_TIP";

		private const string EnchantmentTipLocKey = "CRYSTALS_ENCHANTMENT_SHORT_TIP";

		[SerializeField]
		private Transform itemsContainer;

		[SerializeField]
		private AmuletCrystalRarityChanceView itemPrefab;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private TMP_Text currentLevelText;

		[SerializeField]
		private TMP_Text nextLevelText;

		[SerializeField]
		private TMP_Text tipText;

		[Inject]
		private CrystalModel crystalModel;

		private ViewContainer<AmuletCrystalRarityChanceView, CrystalRarityChanceData> chancesContainer;

		[CompilerGenerated]
		private CrystalRarityChancesMode __Mode__BackingField;

		private ComputedAtom<CrystalRarityChancesMode> __Mode;

		private ComputedAtom<int> __CurrentUpgradeLevel;

		private ComputedAtom<int> __NextUpgradeLevel;

		private ComputedAtom<int> __CurrentMetaStatValue;

		private ComputedAtom<int> __NextMetaStatValue;

		private ComputedAtom<List<CrystalRarityChanceData>> __Chances;

		public CrystalRarityChancesMode Mode
		{
            // Native ELF0x16783a0.
            get {
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Mode == null) __Mode = CodeGenAtom.CreatePooled(this, null, () => Mode, false);
                if (!__Mode.DirectEvaluate()) return __Mode.Value;
                return __Mode__BackingField;
            }
            private set {
                // Native ELF0x16784c8.
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Mode != null && !__Mode.CompareAndInvalidate(value)) return;
                __Mode__BackingField = value;
            }
        }

		private string TipLocKey => Mode == CrystalRarityChancesMode.Enchantment ? EnchantmentTipLocKey : BlacksmithingTipLocKey; // Native ELF0x1678c50.

		private string UpgradeKey => Mode == CrystalRarityChancesMode.Enchantment ? "crystal_enchantment" : "crystal_blacksmithing"; // Native ELF0x1678cc0.

		private int CurrentUpgradeLevel { get {
            // Native ELF0x1678d30.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__CurrentUpgradeLevel == null) __CurrentUpgradeLevel = CodeGenAtom.CreatePooled(this, null, () => CurrentUpgradeLevel, false);
            if (!__CurrentUpgradeLevel.DirectEvaluate()) return __CurrentUpgradeLevel.Value;
            return crystalModel.Upgrades.GetLevel(UpgradeKey);
        } }

		private int NextUpgradeLevel { get {
            // Native ELF0x1678e84.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__NextUpgradeLevel == null) __NextUpgradeLevel = CodeGenAtom.CreatePooled(this, null, () => NextUpgradeLevel, false);
            if (!__NextUpgradeLevel.DirectEvaluate()) return __NextUpgradeLevel.Value;
            return unchecked(CurrentUpgradeLevel + 1);
        } }

		private int CurrentMetaStatValue { get {
            // Native ELF0x167894c.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__CurrentMetaStatValue == null) __CurrentMetaStatValue = CodeGenAtom.CreatePooled(this, null, () => CurrentMetaStatValue, false);
            if (!__CurrentMetaStatValue.DirectEvaluate()) return __CurrentMetaStatValue.Value;
            return Math.Max(MinMetaStatValue, NativeInfoInt((Mode == CrystalRarityChancesMode.Enchantment ? crystalModel.Enchantment : crystalModel.Blacksmithing)?.Value ?? 0.0));
        } }

		private int NextMetaStatValue { get {
            // Native ELF0x1678b20.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__NextMetaStatValue == null) __NextMetaStatValue = CodeGenAtom.CreatePooled(this, null, () => NextMetaStatValue, false);
            if (!__NextMetaStatValue.DirectEvaluate()) return __NextMetaStatValue.Value;
            return unchecked(CurrentMetaStatValue + 1);
        } }

		private int MinMetaStatValue => Mode == CrystalRarityChancesMode.Enchantment ? 0 : 1; // Native ELF0x1678fb4.

		private List<CrystalRarityChanceData> Chances { get {
            // Native ELF0x1678fcc.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__Chances == null) __Chances = CodeGenAtom.CreatePooled(this, null, () => Chances, false);
            if (!__Chances.DirectEvaluate()) return __Chances.Value;
            return Mode == CrystalRarityChancesMode.Enchantment ? GetEnchantmentChances() : GetBlacksmithingChances();
        } }

		public void Setup(CrystalRarityChancesMode mode)
		{
            // Native ELF0x1678540.
            Mode = mode;
        }

		protected override void Awake()
		{
            // Native ELF0x1678544.
            base.Awake(); closeButton.onClick.AddListener(Close);
        }

		public override void Initialize()
		{
            // Native ELF0x16785e0.
            base.Initialize(); if (chancesContainer != null) return;
            chancesContainer = new ViewContainer<AmuletCrystalRarityChanceView, CrystalRarityChanceData>(itemsContainer, itemPrefab)
                .BindTo(Lifetime, () => Chances).Setup((view, data, index) => { view.Setup(data, index); view.transform.SetSiblingIndex(unchecked(index + 1)); });
        }

		protected override void Render()
		{
            // Native ELF0x16787f0.
            currentLevelText.text = LocalizationStrings.Get(LevelLocKey).Replace("{value}", CurrentMetaStatValue.ToString());
            nextLevelText.text = LocalizationStrings.Get(LevelLocKey).Replace("{value}", NextMetaStatValue.ToString());
            tipText.text = LocalizationStrings.Get(TipLocKey);
        }

		private List<CrystalRarityChanceData> GetBlacksmithingChances()
		{
            // Native ELF0x1679124.
            var current = CrystalBalance.GetRarityChances(crystalModel.Config, CurrentMetaStatValue);
            var next = CrystalBalance.GetRarityChances(crystalModel.Config, NextMetaStatValue);
            var rows = new List<CrystalRarityChanceData>(crystalModel.Config.RarityWeights.Count);
            foreach (var def in crystalModel.Config.RarityWeights) {
                current.TryGetValue(def.Rarity, out float currentChance); next.TryGetValue(def.Rarity, out float nextChance);
                rows.Add(new CrystalRarityChanceData(def.Rarity, currentChance, nextChance));
            }
            return rows;
        }

		private List<CrystalRarityChanceData> GetEnchantmentChances()
		{
            // Native ELF0x1679484.
            var rows = new List<CrystalRarityChanceData>(crystalModel.Config.RarityWeights.Count);
            foreach (var def in crystalModel.Config.RarityWeights) {
                float current = CrystalBalance.GetEnchantSuccessChance(crystalModel.Config, def.Rarity, (double)CurrentMetaStatValue);
                float next = CrystalBalance.GetEnchantSuccessChance(crystalModel.Config, def.Rarity, (double)NextMetaStatValue);
                rows.Add(new CrystalRarityChanceData(def.Rarity, current / 100f, next / 100f));
            }
            return rows;
        }
        // Native1678aec..1678b18: FCVTZS saturation plus explicit positive-Infinity -> INT_MIN select.
        private static int NativeInfoInt(double value) => double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;

	}
}
