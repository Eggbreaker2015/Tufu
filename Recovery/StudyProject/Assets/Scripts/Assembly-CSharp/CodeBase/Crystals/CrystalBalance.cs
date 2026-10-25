using System;
using System.Collections.Generic;
using Codebase.Simulation;
using Codebase.Unit;

namespace Codebase.Crystals
{
	public static class CrystalBalance
	{
		public static double CalculateBaseStatValue(CrystalConfig config, SimulationData simulationData, int blacksmithing, CrystalRarity rarity, StatType statType, float qualityBonusPercent)
		{
			double normalized = Math.Max(0.0, ((double)blacksmithing - 1.0) / (double)config.SnapshotMaxBlacksmithing);
			double powered = Math.Pow(normalized, (double)config.SnapshotPower);
			float snapshot = (float)(powered * (double)config.SnapshotMaxMinute + (double)config.SnapshotBaseMinute + (double)(config.SnapshotRarityBonus * (float)rarity)) / 30f;
			double value = simulationData.GetStatValueAtTimeChunk(snapshot, statType);
			double startValue = simulationData.GetStatValueAtTimeChunk(config.SnapshotStartBonusMinute / 30f, statType);
			double startBonus = startValue * (double)config.SnapshotStartBonusMultiplier;
			float colorMultiplier = GetColorStatMultiplier(config, CrystalColors.GetForStat(statType));
			float rarityMultiplier = config.GetRarityStatMultiplier(rarity);
			return startBonus + ((value * (double)colorMultiplier) * (double)rarityMultiplier) * (double)(qualityBonusPercent / 100f + 1f);
		}

		public static CrystalRarity RollRarity(CrystalConfig config, int blacksmithing, Random random)
		{
			var chances = GetRarityChances(config, blacksmithing);
			float roll = (float)random.NextDouble();
			float accumulated = 0f;
			foreach (var chance in chances)
			{
				accumulated += chance.Value;
				if (!(accumulated < roll))
					return chance.Key;
			}

			return CrystalRarity.Common;
		}

		public static Dictionary<CrystalRarity, float> GetRarityChances(CrystalConfig config, int blacksmithing)
		{
			// Native ELF0x1519a30, comparison closure0x151b664 sorts by rarity enum.
			var weights = CalculateRarityWeights(config, blacksmithing, out float total);
			var chances = new Dictionary<CrystalRarity, float>(weights.Count);
			weights.Sort((a, b) => ((int)a.Item1).CompareTo((int)b.Item1));
			float remaining = 1f;
			for (int i = weights.Count - 1; i >= 0; i--)
			{
				float chance = i == 0 ? remaining : (remaining * weights[i].Item2) / 100f;
				chances[weights[i].Item1] = chance;
				remaining = remaining - chance;
			}

			return chances;
		}

		private static List<(CrystalRarity, float)> CalculateRarityWeights(CrystalConfig config, int blacksmithing, out float total)
		{
			// Native ELF0x1519c5c; original out-total is accumulated but not used to normalize the conditional rarity chances.
			var weights = new List<(CrystalRarity, float)>();
			total = 0f;
			foreach (var def in config.RarityWeights)
			{
				float weight = Math.Max(0f, def.BaseWeight + def.BaseGrowValue * (float)blacksmithing);
				int gap = def.MaturityLevel - blacksmithing;
				if (gap > 0)
					weight = (float)((double)weight / Math.Pow((double)def.PreMaturityDecayCoef, (double)gap));
				weights.Add((def.Rarity, weight));
				total = weight + total;
			}

			return weights;
		}

		public static double RescaleBaseStatForEnchantLevel(CrystalConfig config, SimulationData simulationData, double currentValue, int fromLevel, int toLevel, int crystalLevel, CrystalRarity rarity, StatType statType, float efficiencyCoef)
		{
			for (int level = unchecked(fromLevel + 1); level <= toLevel; level = unchecked(level + 1))
				currentValue = ApplyEnchantBaseStatStep(config, simulationData, currentValue, level, crystalLevel, rarity, statType, efficiencyCoef);
			return currentValue;
		}

		public static double ApplyEnchantBaseStatStep(CrystalConfig config, SimulationData simulationData, double currentValue, int enchantLevel, int crystalLevel, CrystalRarity rarity, StatType statType, float efficiencyCoef)
		{
			double flat = GetEnchantFlatSnapshotValue(config, simulationData, enchantLevel, crystalLevel, rarity, statType);
			float multiplier = config.GetEnchantFlatSnapshotLevelMultiplier(enchantLevel);
			return (flat * (double)multiplier + (double)config.EnchantCurrentValueMultiplier * currentValue) * (double)efficiencyCoef + currentValue;
		}

		public static double GetEnchantFlatSnapshotValue(CrystalConfig config, SimulationData simulationData, int enchantLevel, int crystalLevel, CrystalRarity rarity, StatType statType)
		{
			var maturity = config.GetEnchantMaturity(rarity);
			double minute = GetSnapshotMinute(config, unchecked(maturity.MaturityLevel << 1), 0f);
			int offset = config.GetEnchantFlatSnapshotMinuteOffset(enchantLevel);
			double snapshot = simulationData.GetStatValueAtTimeChunk((float)((minute + (double)offset) / 30.0), statType);
			return snapshot * (double)config.GetEnchantFlatSnapshotMultiplier(rarity);
		}

		public static float GetEnchantSuccessChance(CrystalConfig config, CrystalRarity rarity, double enchantmentStatValue)
		{
			var maturity = config.GetEnchantMaturity(rarity);
			double missing = (double)maturity.MaturityLevel - enchantmentStatValue;
			// Original FCMP+B.LE sends unordered NaN to the post-maturity branch.
			if (missing > 0.0)
				return (float)((double)maturity.MaturityChancePercent / Math.Pow((double)config.EnchantPreMaturityPenaltyBase, Math.Pow(missing, 0.9)));
			return (float)Math.Min((double)maturity.MaturityChancePercent - missing * (double)maturity.PostMaturityChanceStepPercent, 100.0);
		}

		public static float GetSpecialStatValue(CrystalConfig config, StatType type, int enchLevel, float efficiencyCoef = 1f)
		{
			if (enchLevel <= 0)
				return 0f;
			var def = GetSpecialStatDef(config, type);
			return def != null && def.ValuesByEnchantLevel.Count != 0 ? def.GetValueAtEnchantLevel(enchLevel) * efficiencyCoef : 0f;
		}

		public static ModifierType GetSpecialStatModifierType(CrystalConfig config, StatType type)
		{
			var def = GetSpecialStatDef(config, type);
			return def != null ? def.ModifierType : (ModifierType)3;
		}

		public static double CalculateSellPrice(CrystalConfig config, CrystalRarity rarity, int blacksmithing, int enchantLevel, LegendaryAbilityType ability, float sellPriceBonusPercent, double legendarySellPriceBonusPercent)
		{
			// Native ELF0x151af9c: config is genuinely unused. Original double constants: ELF8df470=1.2 and8df5a8=.1.
			double powerRarity = Math.Pow((double)rarity, 3.0);
			double powerBlacksmithing = Math.Pow((double)blacksmithing, (double)rarity / 20.0 + 1.2);
			double enchantMultiplier = enchantLevel > 0 ? (((double)enchantLevel * .1 + .1) * (double)enchantLevel + 1.0) : 1.0;
			double legendaryMultiplier = ability == LegendaryAbilityType.SellPriceBonus ? legendarySellPriceBonusPercent / 100.0 + 1.0 : 1.0;
			int next = unchecked((int)rarity + 1);
			double baseValue = powerRarity * 10.0 + powerBlacksmithing * (double)next;
			baseValue = baseValue + (double)unchecked(next * 99);
			double result = ((double)sellPriceBonusPercent / 100.0 + 1.0) * baseValue;
			result = enchantMultiplier * result;
			result = legendaryMultiplier * result;
			return result + (double)unchecked(enchantLevel * 200);
		}

		public static double GetLegendaryAbilityValue(CrystalConfig config, LegendaryAbilityType ability, int enchantLevel = 0)
		{
			int level = Math.Max(0, enchantLevel);
			float value;
			switch (ability)
			{
				case LegendaryAbilityType.SuperCrit:
					value = config.LegendarySuperCritBonus + config.LegendarySuperCritPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.TowerDamageBonus:
					value = config.LegendaryTowerDamageBonus + config.LegendaryTowerDamageBonusPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.TowerDamageReduction:
					value = config.LegendaryTowerDamageReduction + config.LegendaryTowerDamageReductionPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.StatDropBonus:
					value = config.LegendaryStatDropBonus + config.LegendaryStatDropBonusPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.SellPriceBonus:
					value = config.LegendarySellPriceBonus + config.LegendarySellPriceBonusPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.EnhancedEnchant:
					value = config.LegendaryEnhancedEnchantBonus + config.LegendaryEnhancedEnchantBonusPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.DamagePerRageLevel:
					value = config.LegendaryDamagePerRageLevelBase + config.LegendaryDamagePerRageLevelPerEnchant * (float)level;
					break;
				case LegendaryAbilityType.SuperCritChance:
					value = config.LegendarySuperCritChanceBonus + config.LegendarySuperCritChancePerEnchant * (float)level;
					break;
				case LegendaryAbilityType.MaxMarksPerEnchant:
					value = config.LegendaryMaxMarksPerEnchant * (float)unchecked(level + 1);
					break;
				case LegendaryAbilityType.HammerLightningChancePerEnchant:
					value = config.LegendaryHammerLightningChancePerEnchant * (float)unchecked(level + 1);
					break;
				case LegendaryAbilityType.EconomyProcChancePerEnchant:
					value = config.LegendaryEconomyProcChancePerEnchant * (float)unchecked(level + 1);
					break;
				default:
					value = 0f;
					break;
			}

			return (double)value;
		}

		public static double GetLegendaryAbilityValue(CrystalConfig config, CrystalItemModel crystal)
		{
			return crystal != null ? GetLegendaryAbilityValue(config, crystal.LegendaryAbility, crystal.EnchantLevel) : 0.0;
		}

		private static SpecialStatDef GetSpecialStatDef(CrystalConfig config, StatType type)
		{
			foreach (var def in config.SpecialStats)
				if (def.StatType == type)
					return def;
			return null;
		}

		private static float GetColorStatMultiplier(CrystalConfig config, CrystalColor color)
		{
			switch (color)
			{
				case CrystalColor.Red:
					return config.RedCrystalStatMultiplier;
				case CrystalColor.Blue:
					return config.BlueCrystalStatMultiplier;
				case CrystalColor.Green:
					return config.GreenCrystalStatMultiplier;
				default:
					throw new ArgumentOutOfRangeException("color", color, null);
			}
		}

		private static double GetSnapshotMinute(CrystalConfig config, int blacksmithing, float rarityBonus)
		{
			return Math.Pow(((double)blacksmithing - 1.0) / (double)config.SnapshotMaxBlacksmithing, (double)config.SnapshotPower) * (double)config.SnapshotMaxMinute + (double)config.SnapshotBaseMinute + (double)rarityBonus;
		}

		private static double GetEnchantSellPriceMultiplier(int enchantLevel)
		{
			// Native ELF0x151b104, retained as a genuine independently callable helper.
			return enchantLevel > 0 ? (((double)enchantLevel * .1 + .1) * (double)enchantLevel + 1.0) : 1.0;
		}
	}
}
