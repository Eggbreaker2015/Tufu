using Codebase.Unit;
using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class HealFromDamageEffect : AbilityEffectBase
	{
		public bool ScaleWithMissingHealth;

		public override void Execute(AbilityContext context)
		{
            // Native 0x15f06d0: damage-based healing deliberately has no status/VFX call here.
            var unit = context.PlayerUnit;
            double maximum = unit.GetStatValue(StatType.Health);
            if (unit.IsDead) return;
            double scale = ScaleWithMissingHealth ? (double)((1f - (float)(unit.CurrentHealth / unit.GetStatSpecificValue(StatType.Health))) * 100f) : 1.0;
            unit.CurrentHealth = Math.Min(maximum, unit.CurrentHealth + scale * (context.Damage * context.State.GetValue()));
        }
	}
}
