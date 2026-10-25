using System;
using Codebase.Utilities;

namespace Codebase.Meta.Upgrades
{
	public class UpgradeCostDef
	{
		public string CurrencyKey { get; }
		public Func<int, double> AmountByLevel { get; }

		public UpgradeCostDef(string currencyKey, Func<int, double> amountByLevel)
		{
			CurrencyKey = currencyKey;
			AmountByLevel = amountByLevel;
		}

		public double Evaluate(int level)
		{
			return AmountByLevel(level).PrettyRound();
		}
	}
}
