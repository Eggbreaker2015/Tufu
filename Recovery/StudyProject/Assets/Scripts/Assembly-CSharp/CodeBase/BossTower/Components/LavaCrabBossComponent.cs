using Codebase.Enemy;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct LavaCrabBossComponent : IComponent
	{
		public int HitsToExplode;

		public int CurrentHits;

		public float ExplosionDelay;

		public float DamageMultiplier;

		public int ExplosionCount;

		public bool HealOnExplosion;

		public float HealPercent;

		public int LastKnownShockConsumedCount;

		public ParticleSystem ExplosionVfxPrefab;

		public float ExplosionVfxLifetime;

		public EnemySpawnerConfig SpawnerConfig;
	}
}
