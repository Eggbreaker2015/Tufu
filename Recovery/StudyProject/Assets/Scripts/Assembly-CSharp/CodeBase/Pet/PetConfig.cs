using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Pet
{
	// Player-wide bonus once this many species are owned; tiers are cumulative.
	[Serializable]
	public class PetCollectionTier
	{
		public int SpeciesRequired;

		public List<StatModifier> Modifiers = new List<StatModifier>();
	}

	// Pet-only bonus by the number of equipped pets sharing a pet's damage type; only the highest matching tier applies.
	[Serializable]
	public class PetSynergyTier
	{
		public int SameTypeCount;

		public List<StatModifier> Modifiers = new List<StatModifier>();
	}

	public class PetConfig : ScriptableObject
	{
		private const string ResourcePath = "Pet Config";

		private static PetConfig instance;

		private static bool loaded;

		[SerializeField]
		private bool enabled = true;

		public float ReviveCooldown = 15f;

		// Fraction of max health a pet comes back with (cooldown, fountain, ordinary player revive).
		[Range(0f, 1f)]
		public float ReviveHealthFraction = 0.2f;

		public float LeashRadius = 12f;

		public float TeleportDistance = 18f;

		public float EngageRadius = 8f;

		public float FollowSpeedMultiplier = 1.2f;

		// Initial value of the player's Damage stat in pet mode; pets inherit it as part of the player attack.
		public float PlayerBaseAttack = 10f;

		// Share of the weapon's damage a pet keeps as its own attack.
		public float OwnAttackScale = 0.6f;

		// Global scale for pet health, regen, and flat lifesteal.
		public float HealthScale = 0.5f;

		// Obsolete: retained for serialized configs; the level curve no longer uses it.
		public float LevelRatioGrowth = 0.02f;

		// Power curve for pet level growth.
		public float LevelGrowthScale = 0.002f;

		public float LevelGrowthExponent = 2.5f;

		// Ratio factor per item rarity, indexed by Rarity (Common..Mythical).
		public float[] RarityRatioFactor = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.7f, 1.7f };

		public List<PetDefinition> Pets = new List<PetDefinition>();

		public List<PetCollectionTier> CollectionTiers = BuildDefaultCollectionTiers();

		public List<PetSynergyTier> SynergyTiers = BuildDefaultSynergyTiers();

		// Universal shards to unlock an unowned species, indexed by Rarity (Divine unused).
		public int[] UnlockShardCost = { 30, 60, 120, 240, 480, 0, 960 };

		// Universal shards granted per opened chest.
		public int ShardsPerChest = 2;

		// Universal shards per Boss Tower victory.
		public int ShardsPerBossWin = 20;

		// Copies of the defeated boss's mythical pet per Boss Tower victory.
		public int BossPetCopiesPerWin = 2;

		public Sprite BookIconSprite;

		public Sprite ButtonSprite;

		public Sprite PanelSprite;

		public Sprite CloseButtonSprite;

		public Sprite CloseIconSprite;

		public Sprite TabActiveSprite;

		public Sprite TabInactiveSprite;

		public Sprite LockSprite;

		public Sprite ShardSprite;

		// Keyed species in list order.
		public IEnumerable<PetDefinition> Species
		{
			get
			{
				foreach (PetDefinition definition in Pets)
					if (definition != null && !definition.IsFallback)
						yield return definition;
			}
		}

		public static PetConfig Instance
		{
			get
			{
				if (!loaded)
				{
					instance = Resources.Load<PetConfig>(ResourcePath);
					loaded = true;
				}
				return instance;
			}
		}

		public static bool Enabled => true;

		[StaticReset]
		private static void ResetStaticState()
		{
			instance = null;
			loaded = false;
		}

		private static List<PetCollectionTier> BuildDefaultCollectionTiers()
		{
			var tiers = new List<PetCollectionTier>();
			AddCollectionTier(tiers, 5, StatType.Health, ModifierType.PercentAdditive, 0.05f);
			AddCollectionTier(tiers, 10, StatType.GlobalAttack, ModifierType.Flat, 5f);
			AddCollectionTier(tiers, 15, StatType.Health, ModifierType.PercentAdditive, 0.05f);
			AddCollectionTier(tiers, 20, StatType.GlobalAttack, ModifierType.Flat, 5f);
			AddCollectionTier(tiers, 25, StatType.GlobalDefence, ModifierType.Flat, 5f);
			AddCollectionTier(tiers, 30, StatType.Health, ModifierType.PercentAdditive, 0.1f);
			AddCollectionTier(tiers, 36, StatType.GlobalAttack, ModifierType.Flat, 10f);
			return tiers;
		}

		private static void AddCollectionTier(List<PetCollectionTier> tiers, int speciesRequired, StatType statType, ModifierType modifierType, float value)
		{
			var tier = new PetCollectionTier { SpeciesRequired = speciesRequired };
			tier.Modifiers.Add(CreateModifier($"pet_collection.{speciesRequired}.{(int)statType}", statType, modifierType, ModifierSource.PetCollection, value));
			tiers.Add(tier);
		}

		private static List<PetSynergyTier> BuildDefaultSynergyTiers()
		{
			var tiers = new List<PetSynergyTier>();
			AddSynergyTier(tiers, 2, 8f, 0f);
			AddSynergyTier(tiers, 3, 15f, 1.1f);
			AddSynergyTier(tiers, 4, 25f, 1.2f);
			AddSynergyTier(tiers, 5, 40f, 1.3f);
			return tiers;
		}

		// healthMultiplier 0: no health modifier.
		private static void AddSynergyTier(List<PetSynergyTier> tiers, int sameTypeCount, float globalAttack, float healthMultiplier)
		{
			var tier = new PetSynergyTier { SameTypeCount = sameTypeCount };
			tier.Modifiers.Add(CreateModifier($"pet_synergy.{sameTypeCount}.{(int)StatType.GlobalAttack}", StatType.GlobalAttack, ModifierType.Flat, ModifierSource.PetSynergy, globalAttack));
			if (healthMultiplier > 0f)
				tier.Modifiers.Add(CreateModifier($"pet_synergy.{sameTypeCount}.{(int)StatType.Health}", StatType.Health, ModifierType.PercentMultiplicative, ModifierSource.PetSynergy, healthMultiplier));
			tiers.Add(tier);
		}

		private static StatModifier CreateModifier(string name, StatType statType, ModifierType modifierType, ModifierSource source, float value)
		{
			return new StatModifier { Name = name, StatType = statType, ModifierType = modifierType, Source = source, Value = value, StringParam = string.Empty };
		}

		public float GetRarityRatioFactor(Rarity rarity)
		{
			int index = (int)rarity;
			if (RarityRatioFactor == null || index < 0 || index >= RarityRatioFactor.Length)
				return 1f;
			return RarityRatioFactor[index];
		}

		public PetDefinition Find(ItemConfig item)
		{
			if (item == null)
				return null;
			foreach (PetDefinition definition in Pets)
				if (!definition.IsFallback && definition.ItemKey == item.Key)
					return definition;
			PetDefinition firstFallback = null;
			foreach (PetDefinition definition in Pets)
			{
				if (!definition.IsFallback)
					continue;
				if (definition.DamageType == item.DamageType)
					return definition;
				if (firstFallback == null)
					firstFallback = definition;
			}
			if (firstFallback != null)
				return firstFallback;
			// No fallback entries: an unlisted weapon borrows the first species of its damage type.
			foreach (PetDefinition definition in Pets)
				if (definition.DamageType == item.DamageType)
					return definition;
			return null;
		}
	}
}
