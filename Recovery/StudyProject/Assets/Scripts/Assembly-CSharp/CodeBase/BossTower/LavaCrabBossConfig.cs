using System;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class LavaCrabBossConfig : BossBehaviorConfig
	{
		public int HitsToExplode;

		public float ExplosionDelay;

		public float DamageMultiplier;

		public float HealPercent;

		public ParticleSystem ExplosionVfxPrefab;

		public float ExplosionVfxLifetime;
	}
}
