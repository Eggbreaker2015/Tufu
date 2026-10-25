using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;

namespace Codebase.Events
{
	public class EventData : ISaveable
	{
		public DataVar<int> Difficulty;

		public DataVar<int> State;

		public DataVar<int> Progress;

		public DataVar<float> ExtraDuration;

		public DataVar<int> ActivatedAtSeconds;

		public DataVar<WeaponType> WeaponType;

		public DataVar<WeaponType> ResistType;

		private readonly Dictionary<string, DataVar<double>> currencies;

		private readonly Dictionary<string, DataVar<int>> boosterPurchases;

		public void Initialize()
		{
		}

		private void InitializeCurrencies()
		{
		}

		public DataVar<double> GetCurrency(string currencyKey)
		{
			return null;
		}

		public void ResetCurrencies()
		{
		}

		public DataVar<int> GetBoosterPurchases(string boosterKey)
		{
			return null;
		}

		public void ResetBoosters(IEnumerable<string> boosterKeys)
		{
		}
	}
}
