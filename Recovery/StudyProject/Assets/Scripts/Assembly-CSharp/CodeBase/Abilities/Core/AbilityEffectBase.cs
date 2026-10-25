using System;

namespace Codebase.Abilities.Core
{
	[Serializable]
	public abstract class AbilityEffectBase : IAbilityEffect
	{
		public virtual AbilityTrigger? IndependentTrigger => null;

		public virtual bool HasPassiveTick => false;

		public virtual void Execute(AbilityContext context)
		{
		}

		public virtual void UpdatePassive(AbilityContext context, float deltaTime)
		{
		}

		public virtual void OnPassiveInactive(AbilityContext context)
		{
		}
	}
}
