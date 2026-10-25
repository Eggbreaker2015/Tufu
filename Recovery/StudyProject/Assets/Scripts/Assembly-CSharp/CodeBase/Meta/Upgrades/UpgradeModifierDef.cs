using System;
using Codebase.Unit;

namespace Codebase.Meta.Upgrades
{
	public class UpgradeModifierDef
	{
		public string StatKey { get; }
		public ModifierType ModifierType { get; }
		public Func<int, double> ValueByLevel { get; }
		public int Param { get; }
		public string StringParam { get; }

		public UpgradeModifierDef(string statKey, ModifierType modifierType, Func<int, double> valueByLevel, int param = 0, string stringParam = null)
		{
			StatKey = statKey;
			ModifierType = modifierType;
			ValueByLevel = valueByLevel;
			Param = param;
			StringParam = stringParam;
		}

		public double Evaluate(int level)
		{
			return ValueByLevel(level);
		}
	}
}
