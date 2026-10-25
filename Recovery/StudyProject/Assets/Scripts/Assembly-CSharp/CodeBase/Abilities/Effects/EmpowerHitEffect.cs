using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class EmpowerHitEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15ef9c0; default ScalingStat argument is original enum zero.
            context.State.EmpoweredHit += ValueScaler.GetScaledValue(context, ValueScaling);
        }
	}
}
