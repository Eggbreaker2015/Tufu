using Codebase.Enemy;
using Codebase.Player.Inventory;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Events
{
	public struct AbilityTriggerEvent : IEventData
	{
		public AbilityTrigger Trigger;

		public EnemySpawner EnemySpawner;

		public EnemyUnit Target;

		public double Damage;

		public ItemModel Item;

		public bool ItemUnequipped;
	}
}
