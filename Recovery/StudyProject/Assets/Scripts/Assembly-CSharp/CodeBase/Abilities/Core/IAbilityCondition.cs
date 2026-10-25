namespace Codebase.Abilities.Core
{
	public interface IAbilityCondition
	{
		bool Evaluate(AbilityContext context);
	}
}
