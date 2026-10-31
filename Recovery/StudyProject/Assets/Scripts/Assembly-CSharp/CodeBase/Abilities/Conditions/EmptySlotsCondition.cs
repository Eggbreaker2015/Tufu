using System;
using Codebase.Abilities.Core;
using Codebase.Pet;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class EmptySlotsCondition : AbilityConditionBase
	{
		public int MinEmptySlots;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4788. Pet mode: counts empty pet slots only.
            int empty = context.PlayerModel.EmptyPetSlots.Count;
            return empty >= MinEmptySlots;
        }
	}
}
