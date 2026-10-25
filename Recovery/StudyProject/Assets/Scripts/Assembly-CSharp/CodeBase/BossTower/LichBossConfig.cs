using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class LichBossConfig : BossBehaviorConfig
	{
		public float SkeletonInterval;

		public float SkeletonSpawnRadius;

		public int MaxMarks;

		public float SkeletonDeathMarkChance;

		public float SpawnVfxLifetime;

		public float EscapeDistance;

		public float EscapeThreatCooldown;

		public EnemySpawnerConfig SkeletonConfig;

		public ParticleSystem SpawnVfxPrefab;

		public List<SkeletonEvolutionStage> EvolutionStages;

		public LichBossConfig()
		{
			// Original ELF VA 0x15504f0.
			SkeletonInterval = 1f;
			SkeletonSpawnRadius = 3f;
			MaxMarks = 5;
			SkeletonDeathMarkChance = .25f;
			SpawnVfxLifetime = 2f;
			EscapeDistance = 15f;
			EscapeThreatCooldown = 2f;
			EvolutionStages = new List<SkeletonEvolutionStage>();
		}
	}
}
