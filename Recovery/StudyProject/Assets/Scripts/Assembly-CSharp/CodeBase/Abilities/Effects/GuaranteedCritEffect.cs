using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class GuaranteedCritEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f06a4; native flag persists until the ability state is rebuilt.
            context.State.GuaranteedCrit = true;
        }
	}
}
