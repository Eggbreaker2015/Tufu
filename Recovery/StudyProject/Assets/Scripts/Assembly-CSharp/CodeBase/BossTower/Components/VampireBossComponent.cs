using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct VampireBossComponent : IComponent
	{
		public float SpiritInterval;

		public float SpiritTimer;

		public int SpiritCount;

		public float HealPercent;

		public float DamageBoostPercent;

		public float ArmorBoostPercent;

		public float SpeedBoostPerSpirit;

		public float SpiritReachDistance;

		public bool ArmorBoostOnSpiritReach;

		public EnemyUnit SpiritPrefab;

		public ParticleSystem SpiritVfx;

		public EnemySpawnerConfig SpiritConfig;

		public EnemySpawnerConfig SpawnerConfig;

		public float SpiritVfxLifetime;

		public ParticleSystem SpiritSoulVfx;

		public ParticleSystemForceField SpiritSoulForceField;

		public ParticleSystem VampireBuffVfx;

		public ParticleSystem SpiritSpawnVfx;

		public ParticleSystem HandEffectPrefab;

		public bool SpawnPointsPreselected;

		public int[] PreselectedSpawnPointIndices;

		public List<Stat> BakedSpiritStats;

		public int consumedSpirits;
	}
}
