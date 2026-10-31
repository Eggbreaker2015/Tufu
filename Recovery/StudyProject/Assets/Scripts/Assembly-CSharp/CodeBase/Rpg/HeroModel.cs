using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Unit;
using Codebase.Player.Inventory;

namespace Codebase.Rpg
{
	public class HeroModel
	{
		public HeroDefinition Definition;
		public HeroUnit Unit;
		public PetModel Pet;
		public ItemModel Item;
		public ItemSlotModel Slot;
		public ItemModel AttackItem;
		public float AttackCooldown;
		public bool AttackInProgress;
		public EnemyUnit AttackTarget;
		public float TargetOutOfRangeTimer;
		public bool IsDown;
		public float ReviveAt;
		public int SlotOrder;
		public int AppliedStar;
		public float PathFailTimer;
		public EnemyUnit AiTarget;
		public bool IsProtagonist;

		public bool IsAlive => Unit != null && !IsDown && !Unit.IsDead && !(IsProtagonist && Unit.IsDying);
	}
}
