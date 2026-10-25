using System;
using System.Collections.Generic;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class CrystalMilestoneRewards
	{
		public int RequiredCrystals;

		[SerializeReference]
		public List<RewardBase> Rewards;
	}
}
