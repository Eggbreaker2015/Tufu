using Codebase.Enemy;

namespace Codebase.Crystals.Objective
{
	public class CrystalUnit : EnemyUnit
	{
		public int BlacksmithKillReward { get; set; }

		public void PlayHit()
		{
			Codebase.Core.GameContext.Get<CrystalUnitScene>().PlayHit();
		}

		public void PlayDestruction()
		{
			Codebase.Core.GameContext.Get<CrystalUnitScene>().PlayDestruction();
		}
	}
}
