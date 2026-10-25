using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class TargetStunnedCondition : AbilityConditionBase
	{
		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f5014.
            var target = context.PlayerModel.Unit.CurrentTarget;
            return target != null && target.EntityProvider != null && Scellecs.Morpeh.WorldStashExtensions.GetStash<Codebase.Unit.Components.BuffStunComponent>(context.World).Has(target.EntityProvider.Entity);
        }
	}
}
