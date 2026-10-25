using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class TemporaryWeaponBuffEffect : AbilityEffectBase
	{
		public WeaponBuffType BuffType;

		public float Duration = 7f; // Native ctor 0x15f2f90.

		public AbilityValueScaling ValueScaling;

		public bool HideInUI;

		public override bool HasPassiveTick => true;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f2bd4; replace the selected bonus and its timer, default scaling stat is MovementSpeed.
            float value = ValueScaler.GetScaledValue(context, ValueScaling);
            switch (BuffType)
            {
                case WeaponBuffType.Damage: context.State.TemporaryDamageBonus = value; context.State.TemporaryDamageDuration = Duration; break;
                case WeaponBuffType.AttackSpeed: context.State.TemporaryAttackSpeedBonus = value; context.State.TemporaryAttackSpeedDuration = Duration; break;
                case WeaponBuffType.CritDamage: context.State.TemporaryCritBonus = value; context.State.TemporaryCritDuration = Duration; break;
                case WeaponBuffType.CritChance: context.State.TemporaryCritChanceBonus = value; context.State.TemporaryCritChanceDuration = Duration; break;
            }
        }

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15f2ce4. All four stored timers are processed in order, irrespective of this effect's BuffType.
            if (context.State.TemporaryDamageDuration > 0f)
            {
                context.State.TemporaryDamageDuration -= deltaTime;
                if (context.State.TemporaryDamageDuration <= 0f) context.State.TemporaryDamageBonus = 0f;
                else if (context.Owner is Codebase.Player.Inventory.ItemModel damageOwner) damageOwner.AbilityDamageMultiplier *= context.State.TemporaryDamageBonus + 1f;
            }
            if (context.State.TemporaryAttackSpeedDuration > 0f)
            {
                context.State.TemporaryAttackSpeedDuration -= deltaTime;
                if (context.State.TemporaryAttackSpeedDuration <= 0f) context.State.TemporaryAttackSpeedBonus = 0f;
                else if (context.Owner is Codebase.Player.Inventory.ItemModel speedOwner) speedOwner.AbilityAttackSpeedMultiplier *= context.State.TemporaryAttackSpeedBonus + 1f;
            }
            if (context.State.TemporaryCritDuration > 0f)
            {
                context.State.TemporaryCritDuration -= deltaTime;
                if (context.State.TemporaryCritDuration <= 0f) context.State.TemporaryCritBonus = 0f;
                else if (context.Owner is Codebase.Player.Inventory.ItemModel critOwner) critOwner.AbilityCritMultiplier *= context.State.TemporaryCritBonus + 1f;
            }
            if (context.State.TemporaryCritChanceDuration > 0f)
            {
                context.State.TemporaryCritChanceDuration -= deltaTime;
                if (context.State.TemporaryCritChanceDuration <= 0f) context.State.TemporaryCritChanceBonus = 0f;
                else if (context.Owner is Codebase.Player.Inventory.ItemModel chanceOwner) chanceOwner.AbilityCritChanceBonus += context.State.TemporaryCritChanceBonus;
            }
        }
	}
}
