using System;
using Codebase.Abilities.Core;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Inventory;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class DamageForMissingHealthEffect : AbilityEffectBase
	{
		public bool AffectSpecificWeaponType;

		public WeaponType WeaponType;

		// Pet mode only, for weapon (pet) owners: the owner pet itself or the player's claw gets the bonus.
		public MissingHealthBonusReceiver Receiver;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Native 0x15ef3cc: no clamp on missing-health percentage or resulting multiplier.
            // Pet mode: a weapon owner uses its own pet's missing health; armor owners keep the original rule.
            if (PetConfig.Enabled && context.Owner is ItemModel owner && owner.Config.SlotType == ItemSlotType.Weapon)
            {
                ApplyPetOwned(context, owner);
                return;
            }
            float missing = (1f - (float)(context.PlayerUnit.CurrentHealth / context.PlayerUnit.GetStatSpecificValue(Codebase.Unit.StatType.Health))) * 100f;
            float bonus = missing * context.State.GetValue();
            foreach (var item in context.PlayerModel.EquippedWeapons)
                if (!AffectSpecificWeaponType || item.Config.DamageType == WeaponType) item.AbilityDamageMultiplier *= 1f + bonus;
        }

		private void ApplyPetOwned(AbilityContext context, ItemModel owner)
		{
			PetModel pet = GameContext.Get<PetsModel>().Get(owner);
			if (pet == null || !pet.IsAlive)
				return;
			ItemModel receiver = Receiver == MissingHealthBonusReceiver.Player ? context.PlayerModel.PlayerAttackItem : owner;
			if (receiver == null || AffectSpecificWeaponType && receiver.Config.DamageType != WeaponType)
				return;
			// The pet's max health, as PetStatsSyncSystem caps it.
			double maximum = pet.Unit.GetStatValue(Codebase.Unit.StatType.Health);
			if (maximum <= 0.0)
				return;
			float missing = (1f - (float)(pet.Unit.CurrentHealth / maximum)) * 100f;
			receiver.AbilityDamageMultiplier *= 1f + missing * context.State.GetValue();
		}
	}
}
