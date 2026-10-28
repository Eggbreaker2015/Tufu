using System;
using Codebase.Abilities.Core;
using Codebase.Pet;

namespace Codebase.Abilities.Conditions
{
	// Pet mode gate for pet-only abilities (species signatures) on items that also exist with pet mode off.
	[Serializable]
	public class PetModeCondition : AbilityConditionBase
	{
		public override bool Evaluate(AbilityContext context)
		{
			return PetConfig.Enabled;
		}

		public static bool IsPetOnly(AbilityDef def)
		{
			if (def == null || def.Conditions == null)
				return false;
			foreach (IAbilityCondition condition in def.Conditions)
				if (condition is PetModeCondition)
					return true;
			return false;
		}

		// True for a pet-only ability while pet mode is off: it should neither unlock nor be listed.
		public static bool IsHidden(AbilityDef def)
		{
			return !PetConfig.Enabled && IsPetOnly(def);
		}
	}
}
