using System;
using Codebase.Abilities.Core;
using Codebase.Player.Inventory;

namespace Codebase.Abilities.Effects
{
	// Companion of a WeaponStatStackEffect with BuffType CritChance, which nothing reads on its own:
	// adds Value x Stacks to the holding weapon's crit chance every frame.
	[Serializable]
	public class CritChancePerStackEffect : AbilityEffectBase
	{
		public override bool HasPassiveTick => true;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			if (context.State.Stacks > 0 && context.Owner is ItemModel item)
				item.AbilityCritChanceBonus += context.State.GetValue() * context.State.Stacks;
		}
	}
}
