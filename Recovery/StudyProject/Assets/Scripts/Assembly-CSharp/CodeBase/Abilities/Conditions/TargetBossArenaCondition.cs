using System;
using Codebase.Abilities.Core;
using UnityEngine;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class TargetBossArenaCondition : AbilityConditionBase
	{
		[SerializeField]
		public bool ExcludeBossArenaUnits = true; // Original constructor 0x15f4d70.

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f4bf8.
            var target = AbilityTargetResolver.ResolveAttackerTarget(context); // Pet mode: attacker's target.
            if (target == null || target.EntityProvider == null) return false;
            bool isBoss = Scellecs.Morpeh.WorldStashExtensions.GetStash<Codebase.BossTower.Components.BossArenaUnitTag>(context.World).Has(target.EntityProvider.Entity);
            return ExcludeBossArenaUnits ? !isBoss && !context.PlayerModel.InBossTowerArena : isBoss;
        }
	}
}
