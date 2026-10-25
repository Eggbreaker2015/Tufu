using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class CooldownCondition : AbilityConditionBase
	{
		public float Cooldown;

		public bool ScaleByValue;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4728.
            float previous = context.State.Cooldown;
            if (previous <= 0f) context.State.Cooldown = ScaleByValue ? context.State.GetValue() : Cooldown;
            return previous <= 0f;
        }
	}
}
