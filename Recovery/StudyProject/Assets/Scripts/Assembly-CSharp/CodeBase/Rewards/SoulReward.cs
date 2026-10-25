using System;
using Codebase.Amulet;
using Codebase.Utilities;

namespace Codebase.Rewards
{
	[Serializable]
	public class SoulReward : RewardBase
	{
		[KeysDropdown(typeof(SoulKeys))]
		public string SoulKey;
		public int Amount = 1;
	}
}
