using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct SkeletonVariant
	{
		public EnemyUnit Prefab;

		public ParticleSystem VfxPrefab;

		public float UpgradeTime;

		public List<Stat> BakedStats;
	}
}
