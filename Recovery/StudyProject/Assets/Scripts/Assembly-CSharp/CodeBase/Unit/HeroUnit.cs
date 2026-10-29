using Codebase.Enemy;

namespace Codebase.Unit
{
	public abstract class HeroUnit : UnitCore
	{
		public EnemyUnit CurrentTarget { get; set; }

		public bool IsControlled { get; internal set; }

		public abstract void SetWalking(bool value);
	}
}
