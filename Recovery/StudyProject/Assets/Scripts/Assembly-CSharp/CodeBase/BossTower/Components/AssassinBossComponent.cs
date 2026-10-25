using Codebase.Enemy;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Components
{
	public struct AssassinBossComponent : IComponent
	{
		public float IllusionInterval;

		public float IllusionTimer;

		public int IllusionCount;

		public float DamageMultiplier;

		public float IllusionSpawnRadius;

		public float CorrectIllusionScale;

		public bool IsReal;

		public EnemyUnit Prefab;

		public EnemySpawnerConfig SpawnerConfig;
	}
}
