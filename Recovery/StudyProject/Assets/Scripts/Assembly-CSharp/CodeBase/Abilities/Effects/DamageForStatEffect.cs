using System;
using Codebase.Abilities.Core;
using Codebase.Player.Inventory;
using Codebase.Unit;
using Codebase.Utilities;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class DamageForStatEffect : AbilityEffectBase
	{
		public StatType SourceStat;

		public ModifierType ModifierType;

		public bool AffectSpecificSlot;

		[KeysDropdown(typeof(ItemSlotKeys))]
		public string SlotKey;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Native 0x15ef680; ModifierType zero is flat, every nonzero value uses multiplicative damage.
            float bonus = context.State.GetValue() * (float)context.PlayerUnit.GetStatSpecificValue(SourceStat);
            if (AffectSpecificSlot) {
                foreach (var slot in context.PlayerModel.ItemSlots)
                    if (slot.Key == SlotKey) { if (slot.ItemModel != null) ApplyBonus(slot.ItemModel, bonus); return; }
            } else if (ModifierType == ModifierType.Flat) context.PlayerModel.AbilityDamageFlat += bonus;
            else context.PlayerModel.AbilityDamageMultiplier *= 1f + bonus;
        }

		private void ApplyBonus(ItemModel item, float bonus)
		{
            // Native 0x15ef860, bounded ARM64 tail-calls verified past property-setter decompiler spill.
            if (ModifierType == ModifierType.Flat) item.AbilityDamageFlat += bonus;
            else item.AbilityDamageMultiplier *= 1f + bonus;
        }
	}
}
