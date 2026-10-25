using Codebase.Unit;
using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class HealthRegenForMissingHealthEffect : AbilityEffectBase
	{
		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Native 0x15f0ae4: float ratio conversion, then original multiplicative per-frame scalar.
            var unit = context.PlayerUnit;
            float missing = (1f - (float)(unit.CurrentHealth / unit.GetStatSpecificValue(StatType.Health))) * 100f;
            context.PlayerModel.AbilityHealthRegenMultiplier *= missing * context.State.GetValue() + 1f;
        }
	}
}
