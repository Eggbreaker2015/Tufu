using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Simulation.Systems;

namespace Codebase.Player
{
	[Serializable]
	public class RewardPool
	{
		public List<WeightedReward> Reward;

		public WeightedReward GetRandomReward()
		{
			// Original APK ARM64 ELF VA 0x144a610; predicate 0x144aab4.
			if (Reward == null || Reward.Count == 0)
				throw new InvalidOperationException("Reward pool is empty");

			var available = Reward.Where(reward =>
				SimulationSystem.Instance.RPMCalculatedValue[reward.Type] != 0.0).ToList();
			if (available.Count == 0)
				throw new InvalidOperationException("Reward pool is empty");

			float roll = UnityEngine.Random.Range(0f, available.Sum(reward => reward.Weight));
			float cumulativeWeight = 0f;
			foreach (WeightedReward reward in available)
			{
				cumulativeWeight += reward.Weight;
				if (roll <= cumulativeWeight)
					return reward;
			}
			return available[available.Count - 1];
		}
	}
}
