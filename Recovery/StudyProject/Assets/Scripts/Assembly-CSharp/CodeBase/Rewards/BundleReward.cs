using System;
using UnityEngine;

namespace Codebase.Rewards
{
	[Serializable]
	public class BundleReward : RewardBase
	{
		[SerializeReference]
		public RewardBase[] Rewards;
	}
}
