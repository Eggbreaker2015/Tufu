using Codebase.Enemy;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Components
{
	public struct VikingsPendingSpawnComponent : IComponent
	{
		public EnemyUnit DeadViking;

		public VikingsBossComponent VikingData;

		public float Timer;

		public int DeadAllies;
	}
}
