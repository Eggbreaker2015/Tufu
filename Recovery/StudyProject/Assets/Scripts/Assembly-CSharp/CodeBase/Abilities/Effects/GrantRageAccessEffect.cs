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
			{ rageModel.MaxAccessibleLevel = 0; rageModel.RageLevel = 0; rageModel.RagePoints = 0f; return; }
		}
	}
}
