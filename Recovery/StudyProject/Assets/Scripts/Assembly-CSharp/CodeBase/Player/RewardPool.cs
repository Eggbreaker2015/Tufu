using System;
using System.Collections.Generic;

namespace Codebase.Player
{
	[Serializable]
	public class RewardPool
	{
		public List<WeightedReward> Reward;

		public WeightedReward GetRandomReward()
		{
			return null;
		}
	}
}
