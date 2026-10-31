using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Pet;
using Codebase.Player.Inventory;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class TriggerHighestDamageWeaponEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f3550; unlike ResetCooldownEffect this original trigger emits no cooldown-reset event.
            // Pet mode: only weapons whose pet is alive can be picked.
            List<ItemModel> weapons = context.PlayerModel.EquippedWeapons;
            weapons = Codebase.Core.GameContext.Get<PetsModel>().GetAliveWeapons(weapons);
            ItemModel weapon = FindHighestDamageWeapon(weapons);
            if (weapon == null) return;
            weapon.ResetCooldown = true;
            weapon.NextHitEmpower = ValueScaler.GetScaledValue(context, ValueScaling);
        }

		private static ItemModel FindHighestDamageWeapon(List<ItemModel> weapons)
		{
            // Original ELF VA 0x15f35ac, compare actual DPS and retain the first exact tie.
            ItemModel highest = null;
            double maximum = double.MinValue;
            for (int i = 0; i < weapons.Count; i++)
            {
                ItemModel weapon = weapons[i];
                if (maximum < weapon.DPS) { maximum = weapon.DPS; highest = weapon; }
            }
            return highest;
        }
	}
}
