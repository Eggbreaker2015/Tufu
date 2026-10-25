using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AllWeaponsDamageMultiplierEffect : AbilityEffectBase
	{
		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ed784. Reevaluate the original state value for each actual equipped weapon.
            foreach (var weapon in context.PlayerModel.EquippedWeapons)
                weapon.AbilityDamageMultiplier *= context.State.GetValue() + 1f;
        }
	}
}
