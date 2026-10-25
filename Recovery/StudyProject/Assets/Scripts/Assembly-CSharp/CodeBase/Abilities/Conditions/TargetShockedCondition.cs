using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class TargetShockedCondition : AbilityConditionBase
	{
		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4edc.
            var target = context.PlayerModel.Unit.CurrentTarget;
            if (target == null || target.EntityProvider == null) return false;
            ref var shock = ref Scellecs.Morpeh.WorldStashExtensions.GetStash<Codebase.Unit.Components.ShockComponent>(context.World).Get(target.EntityProvider.Entity, out bool exists);
            return exists && shock.IsShocked;
        }
	}
}
