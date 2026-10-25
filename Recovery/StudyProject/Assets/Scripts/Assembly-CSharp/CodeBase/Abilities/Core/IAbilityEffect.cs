namespace Codebase.Abilities.Core
{
	public interface IAbilityEffect
	{
		AbilityTrigger? IndependentTrigger { get; }

		bool HasPassiveTick { get; }

		void Execute(AbilityContext context);

		void UpdatePassive(AbilityContext context, float deltaTime);

		void OnPassiveInactive(AbilityContext context);
	}
}
