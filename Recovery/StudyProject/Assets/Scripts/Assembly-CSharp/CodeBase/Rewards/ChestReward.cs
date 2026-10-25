using System;
using Codebase.Utilities;

namespace Codebase.Rewards
{
	[Serializable]
	public class ChestReward : RewardBase
	{
		[KeysDropdown(typeof(ChestKeys))]
		public string Key;

		public int Amount;
	}
}
