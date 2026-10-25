using System;
using Codebase.Abilities.Core;
using Codebase.Enemy;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class WeaponTypeCondition : AbilityConditionBase
	{
		public WeaponType RequiredType;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f5190.
            return context.Owner is Codebase.Player.Inventory.ItemModel item && item.Config.DamageType == RequiredType;
        }
	}
}
