using System;

namespace Codebase.Abilities.Core
{
	[Serializable]
	public abstract class AbilityConditionBase : IAbilityCondition
	{
		public abstract bool Evaluate(AbilityContext context);
	}
}
