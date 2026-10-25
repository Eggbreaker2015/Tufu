using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class CombatCondition : AbilityConditionBase
	{
		public CombatMode Mode;

		public float Timeout = 5f; // Original constructor 0x15f44a4.

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f43e8.
            var unit = context.PlayerModel.Unit;
            bool inCombat = (double)UnityEngine.Time.time - Math.Max(unit.LastDamageGet, unit.LastDamageDealt) < Timeout;
            return inCombat != ((int)Mode != 0);
        }
	}
}
