using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Inventory;
using UnityEngine;

namespace Codebase.Pet
{
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

		// Ratio growth per effective level above 1.
		public float LevelRatioGrowth = 0.02f;

		// Ratio factor per item rarity, indexed by Rarity (Common..Mythical).
		public float[] RarityRatioFactor = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.7f, 1.7f };

		public List<PetDefinition> Pets = new List<PetDefinition>();

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

		// Pet mode switch; without the asset the game runs the original weapon logic.
		public static bool Enabled => Instance != null && Instance.enabled;

		[StaticReset]
		private static void ResetStaticState()
		{
			instance = null;
			loaded = false;
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
			return firstFallback;
		}
	}
}
