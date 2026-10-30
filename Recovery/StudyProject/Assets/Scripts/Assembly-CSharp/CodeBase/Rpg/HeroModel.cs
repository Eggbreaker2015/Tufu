using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Unit;

namespace Codebase.Rpg
{
	public class HeroModel
	{
		public HeroDefinition Definition;
		public HeroUnit Unit;
		public PetModel Pet;
		public int SlotOrder;
		public int AppliedStar;
		public float PathFailTimer;
		public EnemyUnit AiTarget;
		public bool IsProtagonist;

		public bool IsAlive => IsProtagonist ? Unit != null && !Unit.IsDying && !Unit.IsDead : Pet != null && Pet.IsAlive;
	}
}
