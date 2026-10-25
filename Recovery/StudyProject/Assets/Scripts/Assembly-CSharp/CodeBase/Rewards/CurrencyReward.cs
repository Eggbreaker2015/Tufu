using System;
using Codebase.Economy;
using Codebase.Utilities;

namespace Codebase.Rewards
{
	[Serializable]
	public class CurrencyReward : RewardBase
	{
		[KeysDropdown(typeof(CurrencyKeys))]
		public string Key;

		public double Amount;
	}
}
