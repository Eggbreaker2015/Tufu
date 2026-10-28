using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Player.Models;

namespace Codebase.Pet
{
	// Species collection (player-wide tiers) and same-type synergy (pet-only tiers) lookups shared by the systems and the UI.
	public static class PetCollection
	{
		// Owned weapon items whose config has its own (keyed) species; fallback species do not count.
		public static int CountOwnedSpecies(IReadOnlyList<ItemModel> items)
		{
			PetConfig config = PetConfig.Instance;
			if (config == null || items == null)
				return 0;
			int count = 0;
			for (int i = 0; i < items.Count; i++)
			{
				ItemModel item = items[i];
				if (item == null || item.Config == null || item.IsArmor)
					continue;
				PetDefinition definition = config.Find(item.Config);
				if (definition != null && !definition.IsFallback && definition.ItemKey == item.Config.Key)
					count++;
			}
			return count;
		}

		public static int TotalSpecies
		{
			get
			{
				PetConfig config = PetConfig.Instance;
				if (config == null)
					return 0;
				int count = 0;
				foreach (PetDefinition definition in config.Species)
					count++;
				return count;
			}
		}

		// Cumulative: every tier the owned count reaches.
		public static IEnumerable<PetCollectionTier> ActiveTiers(int owned)
		{
			PetConfig config = PetConfig.Instance;
			if (config == null || config.CollectionTiers == null)
				yield break;
			foreach (PetCollectionTier tier in config.CollectionTiers)
				if (tier != null && owned >= tier.SpeciesRequired)
					yield return tier;
		}

		// Lowest tier not reached yet; null when all are active.
		public static PetCollectionTier NextTier(int owned)
		{
			PetConfig config = PetConfig.Instance;
			if (config == null || config.CollectionTiers == null)
				return null;
			PetCollectionTier next = null;
			foreach (PetCollectionTier tier in config.CollectionTiers)
				if (tier != null && tier.SpeciesRequired > owned && (next == null || tier.SpeciesRequired < next.SpeciesRequired))
					next = tier;
			return next;
		}

		// Highest tier the count reaches; null below the first one.
		public static PetSynergyTier GetSynergyTier(int sameTypeCount)
		{
			PetConfig config = PetConfig.Instance;
			if (config == null || config.SynergyTiers == null)
				return null;
			PetSynergyTier best = null;
			foreach (PetSynergyTier tier in config.SynergyTiers)
				if (tier != null && sameTypeCount >= tier.SameTypeCount && (best == null || tier.SameTypeCount > best.SameTypeCount))
					best = tier;
			return best;
		}

		public static int GetSynergyTierIndex(PetSynergyTier tier)
		{
			PetConfig config = PetConfig.Instance;
			return tier == null || config == null || config.SynergyTiers == null ? -1 : config.SynergyTiers.IndexOf(tier);
		}

		// Equipped weapon slots whose species has this damage type; a dead pet's slot still counts.
		public static int CountEquippedOfType(WeaponType type)
		{
			PlayerModel playerModel = GameContext.Get<PlayerModel>();
			PetConfig config = PetConfig.Instance;
			if (playerModel == null || config == null || playerModel.ItemSlots == null)
				return 0;
			int count = 0;
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
			{
				if (slot.SlotType != ItemSlotType.Weapon || !slot.IsEquipped)
					continue;
				if (GetDamageType(slot.ItemModel, config) == type)
					count++;
			}
			return count;
		}

		public static WeaponType GetDamageType(ItemModel item, PetConfig config)
		{
			PetDefinition definition = config != null ? config.Find(item.Config) : null;
			return definition != null ? definition.DamageType : item.Config.DamageType;
		}
	}
}
