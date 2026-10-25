using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class ResetRageOnLethalEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f1028 tail-calls RageModel.ResetRage, preserving its original timer semantics.
            Codebase.Core.GameContext.Get<Codebase.Rage.RageModel>().ResetRage();
        }
	}
}
