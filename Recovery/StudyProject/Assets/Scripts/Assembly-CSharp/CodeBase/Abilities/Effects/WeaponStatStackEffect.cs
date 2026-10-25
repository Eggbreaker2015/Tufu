using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class WeaponStatStackEffect : AbilityEffectBase
	{
		public WeaponBuffType BuffType;

		public float Duration;

		public bool ResetOnTargetChange;

		public override bool HasPassiveTick => true;

		public override void Execute(AbilityContext context)
		{
			// Native ELF VA 0x15f3880. Target-change comparison is against the actual player's current target.
			if (ResetOnTargetChange && context.State.CurrentTarget != null && context.State.CurrentTarget != context.PlayerModel.Unit.CurrentTarget) context.State.Stacks = 0;
			context.State.Stacks = Math.Min(context.State.GetStackLimit(), context.State.Stacks + 1);
			context.State.Duration = Duration;
		}

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Native ELF VA 0x15f39d0; retain negative remaining Duration on expiration.
			if (context.State.Duration > 0f) { context.State.Duration -= deltaTime; if(context.State.Duration <= 0f) context.State.Stacks = 0; }
		}
	}
}
