using Codebase.Enemy;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Components
{
	public struct MageBossComponent : IComponent
	{
		public int HitsToTeleport;

		public int CurrentHitCount;

		public float RetreatDistance;

		public float RetreatDuration;

		public bool HasAttackedThisCycle;

		public EnemySpawnerConfig SpawnerConfig;
	}
}
