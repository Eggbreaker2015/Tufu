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

		public float LeashRadius = 12f;

		public float TeleportDistance = 18f;

		public float EngageRadius = 8f;

		public float FollowSpeedMultiplier = 1.2f;

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
