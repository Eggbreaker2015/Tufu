using System;
using System.Collections.Generic;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.VIP.Features
{
	[Serializable]
	public class DailyChestLevelConfig
	{
		public int Level;

		public Sprite ChestIcon;

		[SerializeReference]
		public List<RewardBase> Rewards;
	}
}
