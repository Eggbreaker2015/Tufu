using System.Collections.Generic;

namespace Codebase.Abilities.Core
{
	public interface IAbilityOwner
	{
		string Key { get; }

		List<AbilityState> AbilityStates { get; }

		bool HasAbility { get; }
	}
}
