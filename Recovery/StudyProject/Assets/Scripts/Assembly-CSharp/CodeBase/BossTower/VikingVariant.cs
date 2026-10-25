using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class VikingVariant
	{
		public EnemyUnit Prefab;
		public WeaponType WeaponType;
		public ArmorConfig ArmorConfig;
		public Vector3 SpawnOffset;
		public float SpawnRotationY;
		public List<MinionStat> MinionStats = new List<MinionStat>();
		public EnemyUnit GhostPrefab;
	}
}
