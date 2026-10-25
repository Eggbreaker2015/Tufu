using System;
using System.Collections.Generic;
using Codebase.Rewards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codebase.LevelPass
{
	[Serializable]
	public class LevelPassTier
	{
		[RequiredListLength(1, 3)]
		public List<int> Islands;

		public LevelPassTrigger Trigger;

		[Min(1f)]
		public int RequiredCount;

		[SerializeReference]
		public RewardBase FreeReward;

		public int PaidRewardMultiplier;

		[SerializeReference]
		public RewardBase PaidReward;
	}
}
