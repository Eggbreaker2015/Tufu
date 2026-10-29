using System;
using System.Collections.Generic;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Pet
{
	[Serializable]
	public class PetDefinition
	{
		// One species per weapon item, so its rarity is the item's; empty key: fallback entry matched by DamageType.
		public string ItemKey;

		public WeaponType DamageType;

		public GameObject Prefab;

		// Visual scale; 0 keeps the enemy prefab's root scale (boss prefabs are too large for a pet).
		public float VisualScale;

		public string DisplayName;

		// Species ratios of the player's inherited stats, before rarity and level (PetStats.GetRatios).
		public float HealthRatio = 1f;

		public float AttackRatio = 1f;

		public float DefenceRatio = 1f;

		// In combat only; following the player uses PetConfig.FollowSpeedMultiplier.
		public float MoveSpeedRatio = 1f;

		// Own attack multiplier, before the pet level adjustment.
		public float OwnAttackMultiplier = 1f;

		// Child paths under the copied Visual to deactivate, besides the always hidden Chains and Signs_root.
		public List<string> HiddenChildren = new List<string>();

		// Boss Tower boss key for mythical boss pets; empty otherwise.
		public string BossKey;

		// Reserved for future synergies.
		public string Faction;

		public bool IsFallback => string.IsNullOrEmpty(ItemKey);
	}
}
