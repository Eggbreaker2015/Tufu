using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class EmptySlotsCondition : AbilityConditionBase
	{
		public int MinEmptySlots;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4788.
            return context.PlayerModel.EmptyItemSlots.Count >= MinEmptySlots;
        }
	}
}
