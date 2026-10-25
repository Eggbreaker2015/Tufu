using System;
using Codebase.Abilities.Core;
using UnityEngine;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class InvertCondition : AbilityConditionBase
	{
		[SerializeReference]
		public IAbilityCondition Condition;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4a68.
            return Condition == null || !Condition.Evaluate(context);
        }
	}
}
