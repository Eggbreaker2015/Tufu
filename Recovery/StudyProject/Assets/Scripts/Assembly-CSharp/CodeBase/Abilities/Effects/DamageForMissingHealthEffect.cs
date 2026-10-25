using System;
using Codebase.Abilities.Core;
using Codebase.Enemy;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class DamageForMissingHealthEffect : AbilityEffectBase
	{
		public bool AffectSpecificWeaponType;

		public WeaponType WeaponType;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Native 0x15ef3cc: no clamp on missing-health percentage or resulting multiplier.
            float missing = (1f - (float)(context.PlayerUnit.CurrentHealth / context.PlayerUnit.GetStatSpecificValue(Codebase.Unit.StatType.Health))) * 100f;
            float bonus = missing * context.State.GetValue();
            foreach (var item in context.PlayerModel.EquippedWeapons)
                if (!AffectSpecificWeaponType || item.Config.DamageType == WeaponType) item.AbilityDamageMultiplier *= 1f + bonus;
        }
	}
}
