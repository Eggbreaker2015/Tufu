using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct HydraBossComponent : IComponent
	{
		public int RemainingSpawns;

		public int SpawnCount;

		public int MaxSplits;

		public float SizeMultiplier;

		public float CurrentScaleMultiplier;

		public float ChildSpawnRadius;

		public EnemyUnit Prefab;

		public EnemyUnit[] SplitPrefabs;

		public EnemySpawnerConfig ParentConfig;

		public Material OriginalMaterial;

		public Vector3 OriginalScale;

		public List<Stat>[] BakedStatsPerLevel;

		public GameObject SpawnProjectilePrefab;

		public float SpawnProjectileSpeed;

		public ParticleSystem DeathVfxPrefab;

		public float DeathVfxLifetime;
	}
}
