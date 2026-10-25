using System;
using Codebase.Unit;

namespace Codebase.Meta.Upgrades
{
	public class UpgradeStatModifierDef
	{
		public StatType StatType { get; }
		public ModifierType ModifierType { get; }
		public Func<int, double> ValueByLevel { get; }
		public int Param { get; }
		public string StringParam { get; }
		public float ValueMultiplier { get; set; }

		public UpgradeStatModifierDef(StatType statType, ModifierType modifierType, Func<int, double> valueByLevel, int param = 0, string stringParam = null)
		{
			ValueMultiplier = 1f;
			StatType = statType;
			ModifierType = modifierType;
			ValueByLevel = valueByLevel;
			Param = param;
			StringParam = stringParam;
		}

		public double Evaluate(int level)
		{
			return ValueByLevel(level) * ValueMultiplier;
		}
	}
}
