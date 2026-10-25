using Codebase.Enemy;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct VikingsBossComponent : IComponent
	{
		public int GroupId;

		public string BossKey;

		public float BuffPerDeadAlly;

		public EnemySpawnerConfig SpawnerConfig;

		public EnemyUnit GhostPrefab;

		public bool IsGhost;

		public int Index;

		public ParticleSystem BuffVfxPrefab;

		public ParticleSystem BuffTargetVfxPrefab;

		public ParticleSystemForceField BuffForceFieldPrefab;

		public ParticleSystem HandEffectPrefab;

		public float DebuffPerRageLevel;

		public float DebuffDuration;

		public float ScaleStep;
	}
}
