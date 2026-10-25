using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using UnityEngine;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class CompositeCondition : AbilityConditionBase
	{
		public ConditionMode Mode;

		[SerializeReference]
		public List<IAbilityCondition> Conditions = new List<IAbilityCondition>(); // Original constructor 0x15f46a0.

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f44b4.
            if (Conditions.Count == 0) return true;
            if ((int)Mode == 0)
            {
                for (int i = 0; i < Conditions.Count; i++) if (!Conditions[i].Evaluate(context)) return false;
                return true;
            }
            for (int i = 0; i < Conditions.Count; i++) if (Conditions[i].Evaluate(context)) return true;
            return false;
        }
	}
}
