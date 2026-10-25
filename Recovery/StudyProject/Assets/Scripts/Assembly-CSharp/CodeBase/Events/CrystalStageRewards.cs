using System;
using System.Collections.Generic;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class CrystalStageRewards
	{
		[SerializeReference]
		public List<RewardBase> Rewards;
	}
}
