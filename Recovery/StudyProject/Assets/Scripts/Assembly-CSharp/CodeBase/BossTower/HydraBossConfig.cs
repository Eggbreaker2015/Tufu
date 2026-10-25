using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class HydraBossConfig : BossBehaviorConfig
	{
		public int MaxSplits;

		public int SpawnCount;

		public float SizeMultiplier;

		public float ChildSpawnRadius;

		public List<EnemyUnit> SplitPrefabs;

		public List<HydraGenerationStats> GenerationStats;

		public GameObject SpawnProjectilePrefab;

		public float SpawnProjectileSpeed;

		public ParticleSystem DeathVfxPrefab;

		public float DeathVfxLifetime;
        // Native ELF VA 0x15500b8. Serialized original assets override these constructor defaults.
        public HydraBossConfig()
        {
            MaxSplits = 3; SpawnCount = 2; SizeMultiplier = .7f; ChildSpawnRadius = 2f;
            SplitPrefabs = new List<EnemyUnit>(); GenerationStats = new List<HydraGenerationStats>();
            SpawnProjectileSpeed = 5f; DeathVfxLifetime = 2f;
        }

	}
}
