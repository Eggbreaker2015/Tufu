using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Pet.Systems
{
	// Applies the reached collection tiers to the player (Source PetCollection); pets inherit them through PetStatsSyncSystem.
	// Runs before PetSpawnSystem so the pets pick the new values up in the same frame.
	public class PetCollectionBonusSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		private PlayerUnit lastUnit;

		private List<ItemModel> lastItems;

		private int lastItemCount = -1;

		private int lastOwned = -1;

		private bool applied;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;
			PlayerUnit unit = playerModel.Unit;
			if (unit == null)
			{
				lastUnit = null;
				return;
			}
			bool unitChanged = unit != lastUnit;
			if (!PetConfig.Enabled)
			{
				// Player stat lists are shared across units, so clear once per unit in case a bonus is still in them.
				if (applied || unitChanged)
					Clear(unit);
				applied = false;
				lastUnit = unit;
				lastItems = null;
				lastOwned = -1;
				return;
			}
			List<ItemModel> items = playerModel.Items;
			int itemCount = items != null ? items.Count : 0;
			if (!unitChanged && applied && items == lastItems && itemCount == lastItemCount)
				return;
			lastUnit = unit;
			lastItems = items;
			lastItemCount = itemCount;
			int owned = PetCollection.CountOwnedSpecies(items);
			if (!unitChanged && applied && owned == lastOwned)
				return;
			lastOwned = owned;
			Apply(unit, owned);
			applied = true;
		}

		private static void Apply(PlayerUnit unit, int owned)
		{
			double previousMaxHealth = unit.GetStatValue(StatType.Health);
			Clear(unit);
			int tierIndex = 0;
			foreach (PetCollectionTier tier in PetCollection.ActiveTiers(owned))
			{
				if (tier.Modifiers != null)
					for (int i = 0; i < tier.Modifiers.Count; i++)
					{
						StatModifier template = tier.Modifiers[i];
						if (template == null || !unit.TryGetStat(template.StatType, out Stat stat))
							continue;
						StatModifier modifier = PetStatInheritance.Copy(template);
						modifier.Source = ModifierSource.PetCollection;
						if (string.IsNullOrEmpty(modifier.Name))
							modifier.Name = $"pet_collection.{tierIndex}.{i}";
						// Same-named entries of several tiers merge (sum) in AddModifier, which is the cumulative bonus.
						stat.AddModifier(modifier);
					}
				tierIndex++;
			}
			// A higher max health heals by the difference, like a pet's ratio change.
			double maxHealth = unit.GetStatValue(StatType.Health);
			if (!unit.IsDead && maxHealth > previousMaxHealth)
				unit.CurrentHealth = Math.Min(unit.CurrentHealth + (maxHealth - previousMaxHealth), maxHealth);
			else if (unit.CurrentHealth > maxHealth)
				unit.CurrentHealth = maxHealth;
		}

		// AddModifier merges by Name+Source+Type, so removal matches by source (every PetCollection name) instead of by reference.
		private static void Clear(PlayerUnit unit)
		{
			foreach (Stat stat in unit.Stats)
			{
				if (stat == null)
					continue;
				for (int i = stat.Modifiers.Count - 1; i >= 0; i--)
					if (i < stat.Modifiers.Count && stat.Modifiers[i].Source == ModifierSource.PetCollection)
						stat.RemoveModifier(stat.Modifiers[i]);
			}
		}
	}
}
