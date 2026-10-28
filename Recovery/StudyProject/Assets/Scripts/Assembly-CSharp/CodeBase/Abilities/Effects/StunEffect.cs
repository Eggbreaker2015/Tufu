using Codebase.UI.Damage;
using Codebase.Core;
using Scellecs.Morpeh;
using Codebase.Unit.Components;
using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class StunEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Native 0x15f2a50: current player target, deliberately independent of context.Target.
            var target = AbilityTargetResolver.ResolveAttackerTarget(context); // Pet mode: attacker's target.
            if (target == null) return;
            context.World.GetStash<BuffStunComponent>().Set(target.EntityProvider.Entity, new BuffStunComponent {Duration = context.State.GetValue()});
            target.ApplyStunAnimation(true);
            GameContext.Get<PlayerDamageEventsViewSystem>().Create(PlayerDamageEventsViewSystem.StatusEffect.Stun);
        }
	}
}
