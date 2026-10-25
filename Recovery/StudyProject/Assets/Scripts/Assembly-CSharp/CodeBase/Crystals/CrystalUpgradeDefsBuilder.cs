using System.Collections.Generic;
using Codebase.Meta.Upgrades;
using System;
using Codebase.Unit;

namespace Codebase.Crystals
{
	public static class CrystalUpgradeDefsBuilder
	{
		public static List<UpgradeDef> Build(CrystalConfig config)
		{
			int baseMaxGeodes = config.BaseMaxGeodes;
			int baseMaxEnchantmentStones = config.BaseMaxEnchantmentStones;
			double ForgeCost(int index)
			{
				double offset, multiplier, power;
				if (index < 37)
				{
					if (index < 18)
					{
						if (index == 2)
							return 150.0;
						offset = -156.0;
						multiplier = 9.128;
					}
					else
					{
						offset = 4500.0;
						multiplier = 5.793;
					}

					power = 1.2;
				}
				else if ((uint)index < 82u)
				{
					offset = 5500.0;
					multiplier = 5.47;
					power = 1.2;
				}
				else
				{
					offset = -210000.0;
					multiplier = 17.475;
					power = 1.3;
				}

				return Math.Pow((double)unchecked(index - 1), power) * 50.0 * multiplier + offset;
			}

			return new List<UpgradeDef>
			{
				new UpgradeDef("crystal_blacksmithing").WithMaxLevel(78).WithStartingLevel(1).WithCost("crystal.magic_dust", level =>
				{
					double value = ForgeCost(unchecked(level * 2 - 2));
					return value + value;
				}).WithModifier("crystal_blacksmithing", (ModifierType)0, level => (double)level, 0),
				new UpgradeDef("crystal_sell_price_bonus").WithMaxLevel(50).WithCost("crystal.magic_dust", level => Math.Pow((double)level, 2.4) * 50.0 * 1.362 + 32.0).WithModifier("crystal_sell_price_bonus", (ModifierType)0, level => (double)level * 0.05, 0).WithDisplay((UpgradeValueFormat)1, value => value * 100.0),
				new UpgradeDef("crystal_slot2_unlock").WithMaxLevel(1).WithCost("crystal.magic_dust", 2000.0).WithModifier("feature_unlock", (ModifierType)0, 1.0, "crystal_slot_2"),
				new UpgradeDef("crystal_max_geodes").WithMaxLevel(15).WithCost("crystal.magic_dust", level => Math.Pow((double)level, 2.0) * 50.0 * 64.84 + -2274.0).WithModifier("crystal_max_geodes", (ModifierType)0, level => (double)level, 0).WithDisplay((UpgradeValueFormat)2, value => (double)baseMaxGeodes + value),
				new UpgradeDef("crystal_geode_timer").WithMaxLevel(3).WithCost("crystal.magic_dust", level => Math.Pow((double)level, 1.4) * 50.0 * 434.0 + -1739.0).WithModifier("crystal_geode_timer", (ModifierType)0, level => (double)unchecked(level << 2), 0).WithDisplay((UpgradeValueFormat)4),
				new UpgradeDef("crystal_crystals_quality").WithMaxLevel(10).WithCost("crystal.magic_dust", level => Math.Pow((double)level, 2.0) * 50.0 * 171.0 + 1393.0).WithModifier("crystal_crystals_quality", (ModifierType)0, level => (double)level * 0.025, 0).WithDisplay((UpgradeValueFormat)1, value => value * 100.0),
				new UpgradeDef("crystal_enchantment_stones").WithMaxLevel(2).WithCost("crystal.magic_dust", level => (double)unchecked(level * 1000000 - 998000)).WithModifier("crystal_max_enchant_stones", (ModifierType)0, level => (double)level, 0).WithDisplay((UpgradeValueFormat)2, value => (double)baseMaxEnchantmentStones + value),
				new UpgradeDef("crystal_enchantment").WithMaxLevel(45).WithCost("crystal.magic_dust", level =>
				{
					int index = unchecked((level << 1) | 1);
					if (level > 25)
						index = unchecked(level * 5 - 74);
					double value = ForgeCost(index);
					return value + value;
				}).WithModifier("crystal_enchantment", (ModifierType)0, level => (double)level, 0),
				new UpgradeDef("crystal_enchant_efficiency").WithMaxLevel(30).WithCost("crystal.magic_dust", level => Math.Pow((double)level, 2.0) * 50.0 * 14.39 + 249.0).WithModifier("crystal_enchant_efficiency", (ModifierType)4, level => (double)level * 0.01, 0).WithDisplay((UpgradeValueFormat)1, value => value * 100.0),
			};
		}
	}
}
