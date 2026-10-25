using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BonusDamageToStunnedEffect : AbilityEffectBase
	{
		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ee71c. Native first sums base/bonus/next, then adds the old scalar.
            context.PlayerModel.BonusDamageToStunned += context.State.GetValue();
        }
	}
}
