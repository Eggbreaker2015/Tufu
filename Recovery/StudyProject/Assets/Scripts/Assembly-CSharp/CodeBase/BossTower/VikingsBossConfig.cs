using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class VikingsBossConfig : BossBehaviorConfig
	{
		public float BuffPerDeadAlly = .5f;
		public float AggroRange = 15f;
		public float AggroDuration = 10f;
		public float DebuffPerRageLevel = .1f;
		public float DebuffDuration = 5f;
		public float ScaleStep = .2f;
		public List<VikingVariant> AdditionalVikings = new List<VikingVariant>();
		public EnemyUnit GhostViewPrefab;
		public ParticleSystem BuffVfxPrefab;
		public ParticleSystem BuffTargetVfxPrefab;
		public ParticleSystemForceField BuffForceFieldPrefab;
		public ParticleSystem HandEffectPrefab;
	}
}
