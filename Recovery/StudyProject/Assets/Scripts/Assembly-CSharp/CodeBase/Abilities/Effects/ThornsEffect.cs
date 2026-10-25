using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class ThornsEffect : AbilityEffectBase
	{
		public StatType DamageSourceStat;

		public StatType DamageMultiplierStat;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Native 0x15f2fa0: cast each double stat separately before the original float products.
            float source = (float)context.PlayerModel.Unit.GetStatSpecificValue(DamageSourceStat);
            float multiplier = (float)context.PlayerModel.Unit.GetStatSpecificValue(DamageMultiplierStat);
            context.PlayerModel.ThornsDamage += source * multiplier * context.State.GetValue();
        }
	}
}
