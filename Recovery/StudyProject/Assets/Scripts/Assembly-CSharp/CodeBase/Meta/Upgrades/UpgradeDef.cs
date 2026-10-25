using System;
using System.Collections.Generic;
using Codebase.Unit;
using Codebase.Utilities;

namespace Codebase.Meta.Upgrades
{
	public class UpgradeDef
	{
		private readonly List<UpgradeCostDef> costs;
		private readonly List<UpgradeModifierDef> modifiers;
		private readonly List<UpgradeStatModifierDef> statModifiers;
		public string Key { get; }
		public int MaxLevel { get; private set; }
		public int StartingLevel { get; private set; }
		public float CostMultiplier { get; private set; }

		public IReadOnlyList<UpgradeCostDef> Costs
		{
			get
			{
				return costs;
			}
		}

		public IReadOnlyList<UpgradeModifierDef> Modifiers
		{
			get
			{
				return modifiers;
			}
		}

		public IReadOnlyList<UpgradeStatModifierDef> StatModifiers
		{
			get
			{
				return statModifiers;
			}
		}

		public UpgradeValueFormat ValueFormat { get; private set; }
		public Func<double, double> ValueTransform { get; private set; }
		public int? DisplayParam { get; private set; }
		public int SimTargetLevel { get; private set; }
		public float SimWeight { get; private set; }
		public int SimDay { get; private set; }
		public float SimStatSnapshotPercent { get; private set; }
		public float SimCostShare { get; private set; }

		public UpgradeDef(string key)
		{
			costs = new List<UpgradeCostDef>();
			modifiers = new List<UpgradeModifierDef>();
			statModifiers = new List<UpgradeStatModifierDef>();
			MaxLevel = 1;
			CostMultiplier = 1f;
			SimTargetLevel = 1;
			SimWeight = 1f;
			SimDay = 1;
			SimStatSnapshotPercent = 1f;
			SimCostShare = 1f;
			Key = key;
		}

		public UpgradeDef WithMaxLevel(int maxLevel)
		{
			MaxLevel = maxLevel;
			return this;
		}

		public UpgradeDef WithStartingLevel(int startingLevel)
		{
			StartingLevel = startingLevel;
			return this;
		}

		public UpgradeDef WithCostMultiplier(float multiplier)
		{
			CostMultiplier = multiplier;
			return this;
		}

		public UpgradeDef WithStatModifierMultiplier(float multiplier)
		{
			foreach (UpgradeStatModifierDef modifier in statModifiers)
				modifier.ValueMultiplier = multiplier;
			return this;
		}

		public UpgradeDef WithDisplay(UpgradeValueFormat format, Func<double, double> transform = null)
		{
			ValueFormat = format;
			ValueTransform = transform;
			DisplayParam = null;
			return this;
		}

		public UpgradeDef WithDisplay(UpgradeValueFormat format, int param, Func<double, double> transform = null)
		{
			ValueFormat = format;
			ValueTransform = transform;
			DisplayParam = param;
			return this;
		}

		public double GetDisplayValue(double modifierValue)
		{
			return ValueTransform != null ? ValueTransform(modifierValue) : modifierValue;
		}

		public string FormatDisplayValue(double modifierValue)
		{
			double value = GetDisplayValue(modifierValue);
			switch (ValueFormat)
			{
				case UpgradeValueFormat.BonusPercent:
					return "+" + value.Format() + "%";
				case UpgradeValueFormat.Absolute:
					return value.Format();
				case UpgradeValueFormat.Time:
					return TimeLocalizer.GetFormatTime((float)value, true) ?? string.Empty;
				case UpgradeValueFormat.TimeReduceMinutes:
					return "-" + value.Format() + " min";
				default:
					return "+" + value.Format();
			}
		}

		public string FormatDescriptionValue(double modifierValue)
		{
			double value = GetDisplayValue(modifierValue);
			switch (ValueFormat)
			{
				case UpgradeValueFormat.BonusPercent:
					return value.Format() + "%";
				case UpgradeValueFormat.Time:
					return TimeLocalizer.GetFormatTime((float)value, true);
				default:
					return value.Format();
			}
		}

		public UpgradeDef WithSimulationSettings(int targetLevel = 1, float weight = 1f, int day = 1, float statSnapshotPercent = 1f)
		{
			SimTargetLevel = targetLevel;
			SimWeight = weight;
			SimDay = day;
			SimStatSnapshotPercent = statSnapshotPercent;
			return this;
		}

		public UpgradeDef WithSimulationCostShare(float costShare)
		{
			SimCostShare = costShare;
			return this;
		}

		public UpgradeDef WithCost(string currencyKey, Func<int, double> amountByLevel)
		{
			costs.Add(new UpgradeCostDef(currencyKey, amountByLevel));
			return this;
		}

		public UpgradeDef WithCost(string currencyKey, double amount)
		{
			return WithCost(currencyKey, _ => amount);
		}

		public UpgradeDef WithModifier(string statKey, ModifierType modifierType, Func<int, double> valueByLevel, int param = 0)
		{
			modifiers.Add(new UpgradeModifierDef(statKey, modifierType, valueByLevel, param));
			return this;
		}

		public UpgradeDef WithModifier(string statKey, ModifierType modifierType, double value, int param = 0)
		{
			return WithModifier(statKey, modifierType, _ => value, param);
		}

		public UpgradeDef WithStatModifier(StatType statType, ModifierType modifierType, Func<int, double> valueByLevel, int param = 0)
		{
			statModifiers.Add(new UpgradeStatModifierDef(statType, modifierType, valueByLevel, param));
			return this;
		}

		public UpgradeDef WithStatModifier(StatType statType, ModifierType modifierType, double value, int param = 0)
		{
			return WithStatModifier(statType, modifierType, _ => value, param);
		}

		public UpgradeDef WithModifier(string statKey, ModifierType modifierType, Func<int, double> valueByLevel, string stringParam)
		{
			modifiers.Add(new UpgradeModifierDef(statKey, modifierType, valueByLevel, 0, stringParam));
			return this;
		}

		public UpgradeDef WithModifier(string statKey, ModifierType modifierType, double value, string stringParam)
		{
			return WithModifier(statKey, modifierType, _ => value, stringParam);
		}

		public UpgradeDef WithStatModifier(StatType statType, ModifierType modifierType, Func<int, double> valueByLevel, string stringParam)
		{
			statModifiers.Add(new UpgradeStatModifierDef(statType, modifierType, valueByLevel, 0, stringParam));
			return this;
		}

		public UpgradeDef WithStatModifier(StatType statType, ModifierType modifierType, double value, string stringParam)
		{
			return WithStatModifier(statType, modifierType, _ => value, stringParam);
		}

		public List<UpgradeCostInstance> EvaluateCosts(int level)
		{
			var result = new List<UpgradeCostInstance>(costs.Count);
			foreach (UpgradeCostDef cost in costs)
				result.Add(new UpgradeCostInstance(cost.CurrencyKey, (cost.Evaluate(level) * CostMultiplier).PrettyRound()));
			return result;
		}
	}
}
