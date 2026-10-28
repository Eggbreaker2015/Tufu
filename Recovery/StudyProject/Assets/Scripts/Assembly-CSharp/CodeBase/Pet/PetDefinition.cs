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

		public float HealthMultiplier = 1f;

		public float DamageMultiplier = 1f;

		public float DefenceMultiplier = 1f;

		public float AttackSpeedMultiplier = 1f;

		public bool IsFallback => string.IsNullOrEmpty(ItemKey);
	}
}
