using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class VampireBossConfig : BossBehaviorConfig
	{
		public float SpiritInterval = 8f;

		public int SpiritCount = 4;

		public float HealPercent = .1f;

		public float DamageBoostPercent = .05f;

		public float ArmorBoostPercent = .05f;

		public float SpeedBoostPerSpirit = .5f;

		public float SpiritReachDistance = 3f;

		public EnemyUnit SpiritPrefab;

		public EnemySpawnerConfig SpiritConfig;

		public ParticleSystem SpiritVfx;

		public float SpiritVfxLifetime;

		public ParticleSystem SpiritSoulVfx;

		public ParticleSystemForceField SpiritSoulForceField;

		public ParticleSystem VampireBuffVfx;

		public ParticleSystem SpiritSpawnVfx;

		public ParticleSystem HandEffectPrefab;

		public List<MinionStat> SpiritStats = new List<MinionStat>();
	}
}
