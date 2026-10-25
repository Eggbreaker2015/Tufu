using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class SourceMatchCondition : AbilityConditionBase
	{
		public SourceMatchMode Mode;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4b28.
            return Mode != SourceMatchMode.OwnerOnly || ReferenceEquals(context.SourceItem, context.Owner);
        }
	}
}
