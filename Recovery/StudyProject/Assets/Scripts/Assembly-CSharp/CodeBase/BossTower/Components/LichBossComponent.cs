using Codebase.Enemy;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct LichBossComponent : IComponent
	{
		public float SkeletonInterval;

		public float SkeletonTimer;

		public float SkeletonSpawnRadius;

		public int SkeletonSpawnCount;

		public float SkeletonDeathMarkChance;

		public ParticleSystem SpawnVfxPrefab;

		public float SpawnVfxLifetime;

		public EnemySpawnerConfig SkeletonConfig;

		public EnemySpawnerConfig SpawnerConfig;

		public SkeletonVariant[] EvolutionVariants;

		public int MaxEvolutionLevel;
	}
}
