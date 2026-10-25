using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Shop;
using Codebase.Utilities;
using TMPro;
using UniMob.Core;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ChestsInfoView : PopupView
	{
		private const string LevelLocKey = "REWARD_LEVEL";

		private const string LegendaryChanceLocKey = "SHOP_CHESTS_LEGENDARY_CHANCE";

		private const string TotalBlacksmithLevelLocKey = "SHOP_CHESTS_TOTAL_BLACKSMITH_LEVEL";

		private const string BlacksmithBonusLocKey = "SHOP_CHESTS_BLACKSMITH_BONUS";

		private const float CrystalViewHeight = 1100f;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private RectTransform viewRectTransform;

		[SerializeField]
		private TMP_Text currentLevelText;

		[SerializeField]
		private TMP_Text nextLevelText;

		[SerializeField]
		private TMP_Text legendaryChanceText;

		[SerializeField]
		private Image legendaryRarityIcon;

		[SerializeField]
		private Transform chestsContainer;

		[SerializeField]
		private ChestItemChancesInfoView chestItemPrefab;

		[SerializeField]
		private GameObject chestsInfoRoot;

		[SerializeField]
		private GameObject crystalChestsInfoRoot;

		[SerializeField]
		private TMP_Text totalBlacksmithLevelText;

		[SerializeField]
		private TMP_Text blacksmithBonusText;

		[SerializeField]
		private TMP_Text crystalLegendaryChanceText;

		[SerializeField]
		private Image crystalLegendaryRarityIcon;

		[SerializeField]
		private Transform crystalRaritiesContainer;

		[SerializeField]
		private CrystalChestRarityItemView crystalRarityPrefab;

		[Inject]
		private ChestsModel chestsModel;

		[Inject]
		private ChestsConfig chestsConfig;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private CrystalModel crystalModel;

		private ViewContainer<ChestItemChancesInfoView, ChestChancesData> chestsViewContainer;

		private ViewContainer<CrystalChestRarityItemView, ChestRarityChanceData> crystalRaritiesViewContainer;

		[CompilerGenerated]
		private IReadOnlyList<string> __ChestKeys__BackingField = Array.Empty<string>();

		private float defaultViewHeight;

		private ComputedAtom<IReadOnlyList<string>> __ChestKeys;

		private ComputedAtom<IReadOnlyList<ChestRarityChanceData>> __CrystalRarities;

		private ComputedAtom<List<ChestChancesData>> __Chests;

		private IReadOnlyList<string> ChestKeys
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ChestKeys == null) __ChestKeys = CodeGenAtom.CreatePooled(this, null, () => ChestKeys, false);
				return __ChestKeys.DirectEvaluate() ? __ChestKeys__BackingField : __ChestKeys.Value;
			}
			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ChestKeys != null && !__ChestKeys.CompareAndInvalidate(value)) return;
				__ChestKeys__BackingField = value;
			}
		}

		private bool IsCrystalOnlyView
		{
			get
			{
				if (ChestKeys.Count == 0) return false;
				foreach (string key in ChestKeys) if (!chestsModel.IsCrystalChest(key)) return false;
				return true;
			}
		}

		private IReadOnlyList<ChestRarityChanceData> CrystalRarities
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CrystalRarities == null) __CrystalRarities = CodeGenAtom.CreatePooled(this, null, () => CrystalRarities, false);
				if (!__CrystalRarities.DirectEvaluate()) return __CrystalRarities.Value;
				foreach (string key in ChestKeys)
					if (chestsConfig.TryGet(key, out ChestDef def) && chestsModel.IsCrystalChest(def.Key))
					{
						var rarities = new List<ChestRarityChanceData>(BuildCrystalChestData(def).Rarities);
						rarities.Sort((a, b) => ((int)a.Rarity).CompareTo((int)b.Rarity));
						return rarities;
					}
				return Array.Empty<ChestRarityChanceData>();
			}
		}

		private List<ChestChancesData> Chests
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Chests == null) __Chests = CodeGenAtom.CreatePooled(this, null, () => Chests, false);
				if (!__Chests.DirectEvaluate()) return __Chests.Value;
				var result = new List<ChestChancesData>(ChestKeys.Count);
				foreach (string key in ChestKeys)
					if (chestsConfig.TryGet(key, out ChestDef def)) result.Add(BuildChestData(def));
				return result;
			}
		}

		public void Setup(IReadOnlyList<string> chestKeys)
		{
			ChestKeys = chestKeys ?? Array.Empty<string>();
			if (IsInitialized) Render();
		}

		protected override void Awake()
		{
			base.Awake();
			defaultViewHeight = viewRectTransform.sizeDelta.y;
			closeButton.onClick.AddListener(Close);
		}

		public override void Initialize()
		{
			base.Initialize();
			if (chestsViewContainer == null)
				chestsViewContainer = new ViewContainer<ChestItemChancesInfoView, ChestChancesData>(chestsContainer, chestItemPrefab)
					.BindTo(Lifetime, () => Chests).Setup((view, data) => view.Setup(data));
			if (crystalRaritiesViewContainer == null)
				crystalRaritiesViewContainer = new ViewContainer<CrystalChestRarityItemView, ChestRarityChanceData>(crystalRaritiesContainer, crystalRarityPrefab)
					.BindTo(Lifetime, () => CrystalRarities).Setup((view, data) => view.Setup(data));
		}

		protected override void Render()
		{
			bool crystalOnly = IsCrystalOnlyView;
			chestsInfoRoot.SetActive(!crystalOnly);
			crystalChestsInfoRoot.SetActive(crystalOnly);
			Vector2 size = viewRectTransform.sizeDelta;
			size.y = crystalOnly ? CrystalViewHeight : defaultViewHeight;
			viewRectTransform.sizeDelta = size;
			if (crystalOnly) { RenderCrystalState(); return; }
			int level = chestsModel.Level;
			currentLevelText.text = LocalizationStrings.Get(LevelLocKey).Replace("{value}", level.ToString());
			nextLevelText.text = LocalizationStrings.Get(LevelLocKey).Replace("{value}", unchecked(level + 1).ToString());
			ApplyLegendaryChanceDisplay(level, legendaryRarityIcon, legendaryChanceText);
		}

		private void RenderCrystalState()
		{
			double value = crystalModel.Blacksmithing.Value;
			int blacksmithing = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue :
				value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
			int total = unchecked(chestsModel.CrystalBlacksmithBonus + blacksmithing);
			totalBlacksmithLevelText.text = LocalizationStrings.Get(TotalBlacksmithLevelLocKey).Replace("{value}", total.ToString());
			blacksmithBonusText.text = LocalizationStrings.Get(BlacksmithBonusLocKey).Replace("{value}", chestsModel.CrystalBlacksmithBonus.ToString());
			ApplyLegendaryChanceDisplay(chestsModel.Level, crystalLegendaryRarityIcon, crystalLegendaryChanceText);
		}

		private void RefreshLegendaryChanceDisplay(int level)
		{
			ApplyLegendaryChanceDisplay(level, legendaryRarityIcon, legendaryChanceText);
		}

		private void ApplyLegendaryChanceDisplay(int level, Image icon, TMP_Text text)
		{
			Color color = itemsProvider.GetRarityColor(Rarity.Legendary);
			string colorCode = itemsProvider.GetRarityColorCode(Rarity.Legendary);
			icon.color = color;
			text.text = BuildLegendaryChanceText(level, colorCode);
		}

		private string BuildLegendaryChanceText(int level, string rarityColorCode)
		{
			bool crystalOnly = IsCrystalOnlyView;
			string current = FormatLegendaryChancePercent(crystalOnly ? chestsModel.GetCrystalLegendaryChance(level) : chestsModel.GetEquipmentLegendaryChance(level));
			int nextLevel = unchecked(level + 1);
			string next = FormatLegendaryChancePercent(crystalOnly ? chestsModel.GetCrystalLegendaryChance(nextLevel) : chestsModel.GetEquipmentLegendaryChance(nextLevel));
			string rarity = LocalizationStrings.Get(string.Format("ITEM_RARITY_{0}", Rarity.Legendary));
			return LocalizationStrings.Get(LegendaryChanceLocKey).Replace("{color}", rarityColorCode).Replace("{rarity}", rarity).Replace("{current}", current).Replace("{next}", next);
		}

		private string FormatLegendaryChancePercent(float chance)
		{
			return (chance * 100f).ToString("0.##", CultureInfo.InvariantCulture);
		}

		private ChestChancesData BuildChestData(ChestDef chestDef)
		{
			if (chestsModel.IsCrystalChest(chestDef.Key)) return BuildCrystalChestData(chestDef);
			int level = chestsModel.Level;
			bool rare = chestsModel.IsRareChest(chestDef.Key);
			var rarities = new List<ChestRarityChanceData>(chestDef.Drops.Count);
			foreach (ChestDropDef drop in chestDef.Drops)
			{
				float chance = rare ? chestsModel.GetRareChestRarityChance(drop.Rarity, level) : GetStaticDropChance(chestDef.Drops, drop);
				int current = chestsModel.GetEquipmentDropCopies(chestDef.Key, drop.ItemType, level);
				int next = chestsModel.GetEquipmentDropCopies(chestDef.Key, drop.ItemType, unchecked(level + 1));
				rarities.Add(new ChestRarityChanceData(drop.Rarity, chance, current, next));
			}
			return new ChestChancesData(chestDef.Key, chestDef.ClosedSprite, LocalizationStrings.Get("SHOP_CHEST_NAME_" + chestDef.Key), rarities);
		}

		private float GetStaticDropChance(IReadOnlyList<ChestDropDef> drops, ChestDropDef drop)
		{
			float total = 0f;
			foreach (ChestDropDef entry in drops) total += entry.Weight;
			// Original FCMP/B.LE returns zero for unordered totals as well.
			return total > 0f ? drop.Weight / total : 0f;
		}

		private ChestChancesData BuildCrystalChestData(ChestDef chestDef)
		{
			int level = chestsModel.Level;
			var rarities = new List<ChestRarityChanceData>
			{
				new ChestRarityChanceData(Rarity.Legendary, chestsModel.GetCrystalChestRarityChance(Rarity.Legendary, level)),
				new ChestRarityChanceData(Rarity.Epic, chestsModel.GetCrystalChestRarityChance(Rarity.Epic, level))
			};
			return new ChestChancesData(chestDef.Key, chestDef.ClosedSprite, LocalizationStrings.Get("SHOP_CHEST_NAME_" + chestDef.Key), rarities);
		}
	}
}
