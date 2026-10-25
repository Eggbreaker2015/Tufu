using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class SkeletonEvolutionStage
	{
		public float UpgradeTime;

		public EnemyUnit Prefab;

		public ParticleSystem VfxPrefab;

		public List<MinionStat> Stats;

		public SkeletonEvolutionStage()
		{
			// Original ELF VA 0x1550460.
			UpgradeTime = 5f;
			Stats = new List<MinionStat>();
		}
	}
}
