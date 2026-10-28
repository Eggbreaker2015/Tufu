using System;
using Codebase.Abilities.Core;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class HealthThresholdCondition : AbilityConditionBase
	{
		public HealthThresholdSubject Subject;

		public HealthThresholdMode Mode;

		public bool UseAbilityChance;

		public bool UseAbilityValue;

		[Range(0f, 1f)]
		public float Threshold;

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f47f8.
            UnitCore unit = ResolveUnit(context);
            if (unit == null) return false;
            double maximum = unit.GetStatSpecificValue(StatType.Health);
            if (maximum <= 0) return false;
            float threshold = UseAbilityChance ? context.State.GetChance() : UseAbilityValue ? context.State.GetValue() : Threshold;
            float ratio = (float)(unit.CurrentHealth / maximum);
            return Mode == HealthThresholdMode.Below ? ratio <= threshold : Mode != HealthThresholdMode.Above || ratio >= threshold;
        }

		private UnitCore ResolveUnit(AbilityContext context)
        {
            // Native ELF VA 0x15f4924: explicit event target, then actual player target.
            // Pet mode: the fallback is the attacker's target.
            if (Subject == HealthThresholdSubject.Target)
                return AbilityTargetResolver.Resolve(context);
            return context.PlayerModel?.Unit;
        }
	}
}
