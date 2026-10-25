using Codebase.Enemy;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct FireElementalBossComponent : IComponent
	{
		public float AreaInterval;

		public float AreaTimer;

		public float AreaDuration;

		public float AreaDamage;

		public float AreaRadius;

		public float AreaDamageInterval;

		public float MeteorInterval;

		public float MeteorTimer;

		public float MeteorDelay;

		public float MeteorDamage;

		public float MeteorSpawnRadius;

		public float MeteorSpawnHeight;

		public float MeteorRadius;

		public GameObject AreaPrefab;

		public GameObject MeteorPrefab;

		public EnemySpawnerConfig SpawnerConfig;
	}
}
