using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BuffNextThornsDamageEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15eef6c. Native first sums base/bonus/next, then adds the old scalar.
            context.PlayerModel.NextThornsDamageBonus += context.State.GetValue();
        }
	}
}
