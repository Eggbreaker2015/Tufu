using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class HitsCountCondition : AbilityConditionBase
	{
		public bool ScaleFromStackLimit;

		public int RequiredHits;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f49e4.
            float required = ScaleFromStackLimit ? context.State.GetStackLimit() : context.State.CalculateScaledValue(RequiredHits);
            int hits = context.State.HitsCounter;
            if (hits >= required) context.State.HitsCounter = 0;
            return hits >= required;
        }
	}
}
