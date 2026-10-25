using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class EmpowerHitAgainstStaticEffect : AbilityEffectBase
	{
		public float Amount;

		public override AbilityTrigger? IndependentTrigger => AbilityTrigger.OnPreHit; // Native 0x15ef908.

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15ef968; explicit context target, authored Amount and no fallback target.
            if (context.Target != null && context.Target.IsStatic) context.State.EmpoweredHit += Amount;
        }
	}
}
