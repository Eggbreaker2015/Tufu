namespace Codebase.Meta.Upgrades
{
	public readonly struct UpgradeCostInstance
	{
		public string CurrencyKey { get; }
		public double Amount { get; }

		public UpgradeCostInstance(string currencyKey, double amount)
		{
			CurrencyKey = currencyKey;
			Amount = amount;
		}
	}
}
