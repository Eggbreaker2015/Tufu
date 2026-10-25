using System.Collections.Generic;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Shop
{
	public class FirstBuyPackRewardsConfig : ScriptableObject
	{
		[SerializeReference]
		public List<RewardBase> Rewards;
	}
}
