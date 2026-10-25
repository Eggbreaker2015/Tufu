using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class DoubleLootEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15ef8dc. Existing PlayerRewardSystem consumes this state on eligible enemy stat rewards.
            context.State.DoubleLoot = true;
        }
	}
}
