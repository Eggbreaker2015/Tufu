using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AttackSpeedForEmptySlotsEffect : AbilityEffectBase
	{
		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ee360; use actual unlocked empty slots, not the treat-as-empty flag.
            int count = context.PlayerModel.EmptyItemSlots.Count;
            float value = context.State.GetValue();
            if (context.Owner is Codebase.Player.Inventory.ItemModel weapon)
                weapon.AbilityAttackSpeedMultiplier = (value * count + 1f) * weapon.AbilityAttackSpeedMultiplier;
        }
	}
}
