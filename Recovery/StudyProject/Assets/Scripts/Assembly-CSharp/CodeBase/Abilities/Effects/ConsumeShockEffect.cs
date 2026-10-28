using Scellecs.Morpeh;
using Codebase.Unit.Components;
using Codebase.Player.Inventory;
using Codebase.Abilities.Systems;
using Codebase.Abilities.Events;
using Codebase.Core;
using System;
using Codebase.Abilities.Core;
using UnityEngine;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class ConsumeShockEffect : AbilityEffectBase
	{
		[Tooltip("If true, the effect triggers on pre hit independently of the ability's configured trigger.")]
		public bool TriggerOnHit;

		public override AbilityTrigger? IndependentTrigger => TriggerOnHit ? AbilityTrigger.OnPreHit : (AbilityTrigger?)null; // Native 0x15eefcc.

		public override void Execute(AbilityContext context)
		{
            // Native 0x15ef044: current target only; no VFX request exists in this native method.
            var target = AbilityTargetResolver.ResolveAttackerTarget(context); // Pet mode: attacker's target.
            if (target == null || target.EntityProvider == null) return;
            var stash = context.World.GetStash<ShockComponent>();
            var entity = target.EntityProvider.Entity;
            ShockComponent shock = stash.Get(entity, out bool exists);
            if (!exists || !shock.IsShocked) return;
            shock.IsShocked = false;
            shock.ConsumedShockCount = unchecked(shock.ConsumedShockCount + 1);
            shock.ConsumedDecayTimer = 10f;
            stash.Set(entity, shock);
            GameContext.Get<AbilityExecuteEventSystem>().ProcessEventImmediate(new AbilityTriggerEvent {Trigger = AbilityTrigger.OnShockConsume, Item = context.Owner as ItemModel, Attacker = context.Attacker});
        }
	}
}
