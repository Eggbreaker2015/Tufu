using System;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Pet
{
	[Serializable]
	public class PetDefinition
	{
		// Empty key: fallback entry matched by DamageType.
		public string ItemKey;

		public WeaponType DamageType;

		public GameObject Prefab;

		public string DisplayName;

		// Species ratios of the player's inherited stats, before rarity and level (PetStats.GetRatios).
		public float HealthRatio = 1f;

		public float AttackRatio = 1f;

		public float DefenceRatio = 1f;

		// In combat only; following the player uses PetConfig.FollowSpeedMultiplier.
		public float MoveSpeedRatio = 1f;

		// Own attack = weapon damage * PetConfig.OwnAttackScale * OwnAttackMultiplier.
		public float OwnAttackMultiplier = 1f;

		public bool IsFallback => string.IsNullOrEmpty(ItemKey);
	}
}
