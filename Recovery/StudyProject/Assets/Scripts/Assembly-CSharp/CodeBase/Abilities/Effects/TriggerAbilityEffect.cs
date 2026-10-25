using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class TriggerAbilityEffect : AbilityEffectBase
	{
		public int TargetAbilityIndex;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f3098. Direct execution bypasses definition conditions and allocates/releases one context per effect.
            var states = context.Owner.AbilityStates;
            if (TargetAbilityIndex < 0 || TargetAbilityIndex >= states.Count) return;
            var target = states[TargetAbilityIndex];
            var effects = target.Def.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                var next = AbilityContext.Rent();
                next.World = context.World; next.Owner = context.Owner; next.State = target; next.PlayerModel = context.PlayerModel;
                next.Target = context.Target; next.EnemySpawner = context.EnemySpawner; next.Damage = context.Damage; next.Trigger = context.Trigger;
                next.SourceItem = context.SourceItem; next.ItemUnequipped = context.ItemUnequipped; next.DeltaTime = context.DeltaTime;
                effects[i].Execute(next);
                AbilityContext.Release(next);
            }
        }
	}
}
