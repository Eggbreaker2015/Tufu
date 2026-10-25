using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class ChanceCondition : AbilityConditionBase
	{
		public AbilityValueScaling Scaling;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f437c.
            return UnityEngine.Random.value < context.State.GetChance() * ValueScaler.Resolve(Scaling, context);
        }
	}
}
