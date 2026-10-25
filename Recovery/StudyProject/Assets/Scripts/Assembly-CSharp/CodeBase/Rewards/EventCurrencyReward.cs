using System;
using Codebase.Events;
using Codebase.Utilities;

namespace Codebase.Rewards
{
	[Serializable]
	public class EventCurrencyReward : RewardBase
	{
		[KeysDropdown(typeof(EventCurrencies))]
		public string Key;

		public double Amount;
	}
}
