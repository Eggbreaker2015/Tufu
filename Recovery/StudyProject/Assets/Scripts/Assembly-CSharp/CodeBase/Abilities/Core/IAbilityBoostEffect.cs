namespace Codebase.Abilities.Core
{
	public interface IAbilityBoostEffect : IAbilityEffect
	{
		int TargetAbilityIndex { get; }
	}
}
