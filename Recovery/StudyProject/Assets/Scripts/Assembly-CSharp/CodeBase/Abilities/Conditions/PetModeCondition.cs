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
			return true;
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

		// Pet-only abilities are available in pet mode.
		public static bool IsHidden(AbilityDef def)
		{
			return false;
		}
	}
}
