using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Player.Inventory;
using Codebase.Rage;
using Codebase.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class GrantRageAccessEffect : AbilityEffectBase
	{
		public int Level = 1; // Native constructor 0x15f0184.

		public override AbilityTrigger? IndependentTrigger => (AbilityTrigger)0; // Native 0x15efa10 is a nonempty Nullable(0).

		public override void Execute(AbilityContext context)
		{
			// Native 0x15efa6c.
			RecalculateMaxLevel(context.PlayerModel.EquippedItems, GameContext.Get<RageModel>());
		}

		public static void RecalculateMaxLevel(IEnumerable<ItemModel> equippedItems, RageModel rageModel)
		{
			// Native 0x15efb00: inspect actual active states, then effects, taking the maximum grant.
			int maximum = 0;
			foreach (ItemModel item in equippedItems)
				foreach (AbilityState state in item.AbilityStates)
					foreach (IAbilityEffect effect in state.Def.Effects)
						if (effect is GrantRageAccessEffect access) maximum = Math.Max(maximum, access.Level);
			rageModel.MaxAccessibleLevel = maximum;
			rageModel.SyncToWeaponAccess();
		}
	}
}
