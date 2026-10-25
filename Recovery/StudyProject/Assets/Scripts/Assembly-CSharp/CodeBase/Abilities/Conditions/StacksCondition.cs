using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class StacksCondition : AbilityConditionBase
	{
		public StacksMode Mode;

		public bool ResetOnTrigger;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4b60.
            int limit = context.State.GetStackLimit();
            if (Mode == StacksMode.NotReachedMax) return context.State.Stacks < limit;
            if (Mode == StacksMode.RequiresStacks)
            {
                context.State.Stacks++;
                if (context.State.Stacks < limit) return false;
                if (ResetOnTrigger) context.State.Stacks = 0;
            }
            return true;
        }
	}
}
