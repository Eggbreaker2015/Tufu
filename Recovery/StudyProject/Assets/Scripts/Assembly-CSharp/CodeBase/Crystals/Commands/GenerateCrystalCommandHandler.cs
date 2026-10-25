using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Unit;
using UniMob;

namespace Codebase.Crystals.Commands
{
	public class GenerateCrystalCommandHandler : ICommandHandler<GenerateCrystalCommand, CrystalItemModel>
	{
		[Inject]
		private CrystalModel crystalModel;
		private readonly Random random;
		public CrystalItemModel Execute(GenerateCrystalCommand command)
		{
			if (!command.SkipGeodeSpend && !crystalModel.TrySpendGeode())
				return null;
			bool forceLegendary = false, forceEpic = false;
			if (!command.SkipGeodeSpend)
			{
				forceLegendary = crystalModel.ShouldForceLegendaryGeode();
				forceEpic = crystalModel.ShouldForceEpicGeode();
				crystalModel.RegisterGeodeOpened();
			}

			int blacksmithing = 1;
			if (crystalModel.Blacksmithing != null)
			{
				double value = crystalModel.Blacksmithing.Value;
				int converted = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
				blacksmithing = Math.Max(1, converted);
			}

			blacksmithing = unchecked(blacksmithing + command.BlacksmithingBonus);
			CrystalRarity rarity = command.ForcedRarity.HasValue ? command.ForcedRarity.Value : forceLegendary ? CrystalRarity.Legendary : forceEpic ? CrystalRarity.Epic : CrystalBalance.RollRarity(crystalModel.Config, blacksmithing, random);
			var pool = GetAvailableStatPool();
			StatType type = pool[random.Next(pool.Count)];
			double baseValue = crystalModel.CalculateBaseStatValue(blacksmithing, rarity, type);
			var data = crystalModel.CreateCrystalUd();
			using (Atom.NoWatch)
			{
				data.Level.Value = blacksmithing;
				data.Rarity.Value = (int)rarity;
				data.BaseStatType.Value = (int)type;
				data.BaseStatValue.Value = baseValue;
				data.Variant.Value = (int)CrystalColors.GetForStat(type);
				if (rarity == CrystalRarity.Legendary)
				{
					var ability = RollLegendaryAbility();
					data.LegendaryAbility.Value = (int)ability;
					if (ability == LegendaryAbilityType.DoubleStat)
					{
						StatType secondary = pool[random.Next(pool.Count)];
						data.SecondaryStatType.Value = (int)secondary;
						data.SecondaryStatValue.Value = crystalModel.CalculateBaseStatValue(blacksmithing, CrystalRarity.Rare, secondary);
					}
				}

				data.IsNew.Value = true;
			}

			var crystal = crystalModel.RegisterCrystal(data);
			for (int i = 0; i < command.EnchantLevel; i++)
				crystalModel.ApplyEnchantLevel(crystal);
			return crystal;
		}

		private List<StatType> GetAvailableStatPool()
		{
			var result = new List<StatType>(crystalModel.Config.BaseStatPool);
			if (crystalModel.MagicStatsUnlocked)
				result.AddRange(crystalModel.Config.MagicStatPool);
			return result;
		}

		private LegendaryAbilityType RollLegendaryAbility()
		{
			var values = new List<LegendaryAbilityType>();
			foreach (LegendaryAbilityType value in Enum.GetValues(typeof(LegendaryAbilityType)))
				if (value != LegendaryAbilityType.None)
					values.Add(value);
			return values[random.Next(values.Count)];
		}

		public GenerateCrystalCommandHandler()
		{
			random = new Random();
		}
	}
}
