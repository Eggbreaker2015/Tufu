using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Crystals
{
	[CreateAssetMenu(menuName = "Devour/CrystalConfig")]
	public class CrystalConfig : ScriptableObject
	{
		public int UnlockMinute;
		public List<CrystalSlotDef> Slots;
		public int BaseGeodeTimerMinutes;
		public int MinGeodeTimerMinutes;
		public int BaseMaxGeodes;
		public int AbsoluteMaxGeodes;
		public int ObjectiveGeodesRequiredForRespawn;
		public float ObjectiveSnapshotBaseDay;
		public float ObjectiveSnapshotDayStep;
		public float ObjectiveReflectionDamageMultiplier;
		public Color ObjectivePoiHighlightColor;
		public int BaseMaxEnchantmentStones;
		public int MaxEnchantLevel;
		public float BaseEnchEfficiencyCoef;
		public float MaxEnchEfficiencyCoef;
		public float EnchantCurrentValueMultiplier;
		public float EnchantPreMaturityPenaltyBase;
		public List<RarityEnchantMaturityDef> EnchantMaturityDefs;
		public List<RarityEnchantFlatSnapshotMultiplierDef> EnchantFlatSnapshotMultipliers;
		public List<EnchantFlatSnapshotMinuteOffsetDef> EnchantFlatSnapshotMinuteOffsets;
		public List<StatType> BaseStatPool;
		public List<StatType> MagicStatPool;
		public List<RarityWeightDef> RarityWeights;
		public List<RarityMultiplierDef> RarityStatMultipliers;
		public float SnapshotBaseMinute;
		public float SnapshotMaxMinute;
		public float SnapshotPower;
		public float SnapshotRarityBonus;
		public int SnapshotMaxBlacksmithing;
		public float SnapshotStartBonusMinute;
		public float SnapshotStartBonusMultiplier;
		public float GreenCrystalStatMultiplier;
		public float BlueCrystalStatMultiplier;
		public float RedCrystalStatMultiplier;
		public float SellPriceMaxBonusPercent;
		public List<SpecialStatDef> SpecialStats;
		public float LegendarySuperCritBonus;
		public float LegendarySuperCritPerEnchant;
		public float LegendaryTowerDamageBonus;
		public float LegendaryTowerDamageBonusPerEnchant;
		public float LegendaryTowerDamageReduction;
		public float LegendaryTowerDamageReductionPerEnchant;
		public float LegendaryStatDropBonus;
		public float LegendaryStatDropBonusPerEnchant;
		public float LegendarySellPriceBonus;
		public float LegendarySellPriceBonusPerEnchant;
		public float LegendaryEnhancedEnchantBonus;
		public float LegendaryEnhancedEnchantBonusPerEnchant;
		public float LegendarySuperCritChanceBonus;
		public float LegendarySuperCritChancePerEnchant;
		[Tooltip("+N max marks per enchant level")]
		public float LegendaryMaxMarksPerEnchant;
		[Tooltip("+X% damage per rage level (X = base + perEnchant * enchantLevel)")]
		public float LegendaryDamagePerRageLevelBase;
		public float LegendaryDamagePerRageLevelPerEnchant;
		[Tooltip("+X% economy ability proc chance per enchant level")]
		public float LegendaryEconomyProcChancePerEnchant;
		[Tooltip("+X% lightning chance per enchant level (for hammers)")]
		public float LegendaryHammerLightningChancePerEnchant;
		public List<LegendaryAbilityIconDef> LegendaryAbilityIcons;
		public int PassExtraMaxGeodes;
		public int PassGeodeTimerReductionSeconds;
		public int PassBlacksmithingBonus;
		public int PassEnchantmentBonus;
		public int PassMaxEnchantStonesBonus;
		public float PassPriceUsd;
		public int PassDurationDays;
		public Sprite PassSlotIcon;
		public Sprite PassGeodesIcon;
		public Sprite PassGeodeTimerIcon;
		public Sprite PassBlacksmithingIcon;
		public Sprite PassEnchantmentIcon;
		public Sprite PassEnchantStonesIcon;
		private void OpenUpgradesBuilder()
		{
		}

		public RarityWeightDef GetRarityWeight(CrystalRarity rarity)
		{
			foreach (var def in RarityWeights)
				if (def.Rarity == rarity)
					return def;
			return null;
		}

		public float GetRarityStatMultiplier(CrystalRarity rarity)
		{
			foreach (var def in RarityStatMultipliers)
				if (def.Rarity == rarity)
					return def.Multiplier;
			return 1f;
		}

		public RarityEnchantMaturityDef GetEnchantMaturity(CrystalRarity rarity)
		{
			foreach (var def in EnchantMaturityDefs)
				if (def.Rarity == rarity)
					return def;
			throw new System.InvalidOperationException(string.Format("Enchant maturity definition is missing for {0}", rarity));
		}

		public float GetEnchantFlatSnapshotMultiplier(CrystalRarity rarity)
		{
			foreach (var def in EnchantFlatSnapshotMultipliers)
				if (def.Rarity == rarity)
					return def.Multiplier;
			throw new System.InvalidOperationException(string.Format("Enchant flat snapshot multiplier definition is missing for {0}", rarity));
		}

		public int GetEnchantFlatSnapshotMinuteOffset(int enchantLevel)
		{
			foreach (var def in EnchantFlatSnapshotMinuteOffsets)
				if (def.EnchantLevel == enchantLevel)
					return def.ExtraMinutes;
			throw new System.InvalidOperationException(string.Format("Enchant flat snapshot minute offset definition is missing for +{0}", enchantLevel));
		}

		public float GetEnchantFlatSnapshotLevelMultiplier(int enchantLevel)
		{
			foreach (var def in EnchantFlatSnapshotMinuteOffsets)
				if (def.EnchantLevel == enchantLevel)
					return def.Multiplier;
			throw new System.InvalidOperationException(string.Format("Enchant flat snapshot level multiplier definition is missing for +{0}", enchantLevel));
		}

		public Sprite GetLegendaryAbilityIcon(LegendaryAbilityType ability)
		{
			foreach (var def in LegendaryAbilityIcons)
				if (def.Ability == ability)
					return def.Sprite;
			return null;
		}

		public LegendaryAbilityIconDef GetLegendaryAbilityDef(LegendaryAbilityType ability)
		{
			foreach (var def in LegendaryAbilityIcons)
				if (def.Ability == ability)
					return def;
			return null;
		}
	}
}
