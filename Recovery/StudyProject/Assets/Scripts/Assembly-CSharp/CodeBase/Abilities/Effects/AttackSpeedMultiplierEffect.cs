using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AttackSpeedMultiplierEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public bool PerEquippedWeapon;

		public override bool HasPassiveTick => true;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ee470; the native FCMP/B.LS permits unordered NaN.
            float value = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);
            if (value <= 0f) return;
            if (PerEquippedWeapon) value *= context.PlayerModel.EquippedWeapons.Count;
            foreach (var weapon in context.PlayerModel.EquippedWeapons)
                weapon.AbilityAttackSpeedMultiplier = (value + 1f) * weapon.AbilityAttackSpeedMultiplier;
        }
	}
}
