using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Pet;
using Codebase.Player.Inventory;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class ResetCooldownEffect : AbilityEffectBase
	{
		public ResetCooldownMode Mode;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f0db8; reset is deferred to the existing attack consumer, with synchronous notification.
            ItemModel weapon;
            if (Mode == ResetCooldownMode.HighestDamageWeapon)
            {
                // Pet mode: only weapons whose pet is alive can be picked.
                List<ItemModel> weapons = context.PlayerModel.EquippedWeapons;
                weapons = Codebase.Core.GameContext.Get<PetsModel>().GetAliveWeapons(weapons);
                weapon = FindHighestDamageWeapon(weapons);
                if (weapon == null) return;
                weapon.ResetCooldown = true;
                weapon.NextHitEmpower = context.State.GetValue();
            }
            else if (Mode == ResetCooldownMode.Self)
            {
                weapon = context.Owner as ItemModel;
                if (weapon == null) return;
                // Pet mode: a weapon whose pet is dead does not reset.
                if (weapon.Config.SlotType == ItemSlotType.Weapon && !Codebase.Core.GameContext.Get<PetsModel>().IsPetAlive(weapon)) return;
                weapon.ResetCooldown = true;
            }
            else return;
            Codebase.Core.GameContext.Get<Codebase.Abilities.Systems.AbilityExecuteEventSystem>().ProcessEventImmediate(
                new Codebase.Abilities.Events.AbilityTriggerEvent { Trigger = AbilityTrigger.OnCooldownReset, Item = weapon });
        }

		private static ItemModel FindHighestDamageWeapon(List<ItemModel> weapons)
		{
            // Original ELF VA 0x15f0f40 selects by DPS, first on ties; NaN never wins.
            ItemModel highest = null;
            double maximum = double.MinValue;
            for (int i = 0; i < weapons.Count; i++)
            {
                ItemModel weapon = weapons[i];
                if (maximum < weapon.DPS)
                {
                    maximum = weapon.DPS;
                    highest = weapon;
                }
            }
            return highest;
        }
	}
}
