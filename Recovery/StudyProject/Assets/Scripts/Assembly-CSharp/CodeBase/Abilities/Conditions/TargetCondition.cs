using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class TargetCondition : AbilityConditionBase
	{
		public TargetConditionMode Mode;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4d80.
            switch (Mode)
            {
                case TargetConditionMode.IsStationary: return context.Target != null && context.Target.IsStatic;
                case TargetConditionMode.IsNotStationary: return context.Target == null || !context.Target.IsStatic;
                case TargetConditionMode.TargetChanged: return context.State.CurrentTarget != AbilityTargetResolver.ResolveAttackerTarget(context); // Pet mode: attacker's target.
                default: return true;
            }
        }
	}
}
