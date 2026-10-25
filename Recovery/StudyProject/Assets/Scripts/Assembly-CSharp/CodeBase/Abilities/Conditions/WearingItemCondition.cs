using System;
using Codebase.Abilities.Core;
using Codebase.Player.Inventory;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class WearingItemCondition : AbilityConditionBase
	{
		public ItemConfig RequiredItem;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f5238.
            if (RequiredItem == null) return false;
            foreach (var item in context.PlayerModel.EquippedItems) if (item.Config == RequiredItem) return true;
            return false;
        }
	}
}
