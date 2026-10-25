using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class WeaponSetCondition : AbilityConditionBase
	{
		public SynergyMode Mode;

		public int RequiredCount;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f5130.
            int count = Mode == SynergyMode.DifferentWeaponTypes ? context.PlayerModel.EquippedDifferentWeaponTypesCount : context.PlayerModel.EquippedSameWeaponTypeMaxCount;
            return count >= RequiredCount;
        }
	}
}
