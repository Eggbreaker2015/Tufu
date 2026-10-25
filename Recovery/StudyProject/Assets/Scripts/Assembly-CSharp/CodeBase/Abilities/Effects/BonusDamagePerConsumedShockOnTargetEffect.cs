using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BonusDamagePerConsumedShockOnTargetEffect : AbilityEffectBase
	{
		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ee6bc; sum base/bonus/next before adding the old scalar.
            context.PlayerModel.DamagePerConsumedShockOnTarget += context.State.GetValue();
        }
	}
}
