using Codebase.Enemy;
using Codebase.Player.Inventory;
using Scellecs.Morpeh;

namespace Codebase.Unit.Events
{
	public struct WeaponHitEvent : IEventData
	{
		public UnitCore Source;

		public UnitCore Target;

		public ItemModel Weapon;

		public WeaponType DamageType;

		public Armor Armor;

		public bool IsCrit;

		public bool IsSuperCrit;

		public bool IsEmpowered;

		public bool Undodgeable;

		public bool HasSpecialMultiplier;

		public double SpecialMultiplier;

		public bool HasFixedDamage;

		public double FixedDamage;
	}
}
