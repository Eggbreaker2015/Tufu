using Codebase.Unit;
using Codebase.Abilities.Conditions;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Rage;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Core
{
	public static class ValueScaler
	{
		public static float Resolve(AbilityValueScaling scaling, AbilityContext context, StatType scalingStat = StatType.MovementSpeed)
		{
			// Native ELF VA 0x15f3ca8; native float conversions occur after the double health ratio.
			switch (scaling)
			{
				case AbilityValueScaling.PerMissingHealthPercent:
					return (1f - (float)(context.PlayerModel.Unit.CurrentHealth / context.PlayerModel.Unit.GetStatSpecificValue(StatType.Health))) * 100f;
				case AbilityValueScaling.PerHealthAbovePercent:
					float ratio = (float)(context.PlayerModel.Unit.CurrentHealth / context.PlayerModel.Unit.GetStatSpecificValue(StatType.Health));
					float above = (ratio - ResolveHealthAboveThreshold(context)) * 100f;
					return above >= 0f ? above : 0f;
				case AbilityValueScaling.PerMarksOnTarget: return ResolveMarksOnTarget(context);
				case AbilityValueScaling.PerRageLevel: return ResolveRageLevel(context);
				case AbilityValueScaling.PerEmptySlot:
					// Pet mode: pet (weapon) slots only.
					return context.PlayerModel.TreatAsAllSlotsEmpty ? CountWeaponSlots(context) : context.PlayerModel.EmptyPetSlots.Count;
					return context.PlayerModel.TreatAsAllSlotsEmpty ? context.PlayerModel.ItemSlots.Count : context.PlayerModel.EmptyItemSlots.Count;
				case AbilityValueScaling.PerConsumedShockOnTarget: return ResolveConsumedShock(context);
				case AbilityValueScaling.PerHitsTaken:
					return System.Math.Min(context.State.HitsCounter, context.State.GetStackLimit());
				case AbilityValueScaling.PerStat: return (float)context.PlayerModel.Unit.GetStatSpecificValue(scalingStat);
				default: return 1f;
			}
		}

		private static float ResolveMarksOnTarget(AbilityContext context)
		{
			// Native ELF VA 0x15f4014.
			UnitCore target = ResolveTarget(context);
			if (target == null || target.EntityProvider == null) return 0f;
			ref MarkComponent mark = ref context.World.GetStash<MarkComponent>().Get(target.EntityProvider.Entity, out bool exists);
			return exists ? mark.Stacks : 0f;
		}

		private static UnitCore ResolveTarget(AbilityContext context)
		{
			// Native ELF VA 0x15f42e8. Pet mode: falls back to the attacker's or owner pet's target.
			return AbilityTargetResolver.Resolve(context);
		}

		private static int CountWeaponSlots(AbilityContext context)
		{
			int count = 0;
			foreach (ItemSlotModel slot in context.PlayerModel.ItemSlots)
				if (slot.SlotType == ItemSlotType.Weapon)
					count++;
			return count;
		}

		private static float ResolveRageLevel(AbilityContext context)
		{
			// Native ELF VA 0x15f4140.
			return GameContext.Get<RageModel>().ActiveRageLevel;
		}

		private static float ResolveConsumedShock(AbilityContext context)
		{
			// Native ELF VA 0x15f41bc.
			UnitCore target = ResolveTarget(context);
			if (target == null || target.EntityProvider == null) return 0f;
			ref ShockComponent shock = ref context.World.GetStash<ShockComponent>().Get(target.EntityProvider.Entity, out bool exists);
			return exists ? shock.ConsumedShockCount : 0f;
		}

		public static float GetScaledValue(AbilityContext context, AbilityValueScaling scaling, StatType scalingStat = StatType.MovementSpeed)
		{
			// Native ELF VA 0x15f2c8c.
			return context.State.GetValue() * Resolve(scaling, context, scalingStat);
		}

		public static float ResolveHealthAboveThreshold(AbilityContext context)
		{
			// Native ELF VA 0x15f3ed0: first fixed player-above condition, otherwise Def.Chance.
			AbilityDef def = context.State.Def;
			if (def?.Conditions != null)
				foreach (var condition in def.Conditions)
					if (condition is HealthThresholdCondition health && health.Subject == HealthThresholdSubject.Player && health.Mode == HealthThresholdMode.Above && !health.UseAbilityChance && !health.UseAbilityValue)
						return health.Threshold;
			return def == null ? 0f : def.Chance;
		}
	}
}
