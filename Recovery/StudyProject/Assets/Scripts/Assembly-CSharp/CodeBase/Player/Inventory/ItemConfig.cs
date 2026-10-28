using System.Collections.Generic;
using Codebase.Abilities;
using Codebase.Enemy;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Player.Inventory
{
	public class ItemConfig : ScriptableObject
	{
		public string Key;

		public Rarity Rarity;

		public ItemSlotType SlotType;

		public WeaponType DamageType;

		public StatType DamageStatTypeFlat;

		public StatType DamageStatTypePercent;

		public float FlatDamage;

		public float AttackCooldown;

		public float SimulationDpsWeaponMultiplier = 1f;

		public int Number;

		[Range(0f, 1f)]
		public float AnimationCastPoint;

		[Range(0f, 1f)]
		public float AnimationBackPoint;

		public Sprite Icon;

		public Sprite DivineIcon;

		public WeaponView Prefab;

		public WeaponView DivinePrefab;

		public List<StatModifier> StatModifiers;

		public List<AbilityDef> AbilityDefs = new List<AbilityDef>();

		// Pet species added for pet mode; excluded from random drops while pet mode is off.
		public bool PetOnly;

		public float HitFrameAt60Fps => (1f - AnimationCastPoint) * AttackCooldown * 60f; // Native 0x14630e8.

		private void OnValidate()
		{
			// Native 0x146310c: authored item modifiers are stamped with their item source.
			foreach (StatModifier modifier in StatModifiers)
			{
				modifier.Name = Key;
				modifier.Source = ModifierSource.Item;
			}
		}
	}
}
