using Codebase.Player.Inventory;
using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class DamageForEmptySlotsEffect : AbilityEffectBase
	{
		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Native 0x15ef2c4: original actual empty-slot count, applied only when owner is an ItemModel.
            int empty = context.PlayerModel.EmptyItemSlots.Count;
            float value = context.State.GetValue();
            if (context.Owner is ItemModel item) item.AbilityDamageMultiplier *= 1f + value * empty;
        }
	}
}
