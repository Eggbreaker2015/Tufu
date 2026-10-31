using System.Collections.Generic;
using Codebase.Abilities;
using Codebase.Player.Inventory;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Pet
{
	// Which player stat modifiers a pet inherits: growth and direct rewards yes, helmet/shield/crystal equipment no.
	public class PetStatInheritance
	{
		private const string CrystalPrefix = "crystal_";

		private static readonly HashSet<string> ExcludedNames = new HashSet<string>
		{
			"graph_rage_extra_health",
			"LavaCrabShockRegen"
		};

		private readonly Dictionary<string, ItemSlotType> ownerSlots = new Dictionary<string, ItemSlotType>();

		private readonly HashSet<string> warnedNames = new HashSet<string>();

		// Item/ability modifier name -> inherited; avoids a Split per modifier on every rebuild.
		private readonly Dictionary<string, bool> ownedNames = new Dictionary<string, bool>();

		public PetStatInheritance(ItemsProvider itemsProvider)
		{
			foreach (ItemConfig item in itemsProvider.Items)
			{
				if (item == null)
					continue;
				// Authored item modifiers are named after the item key (ItemConfig.OnValidate).
				AddOwner(item.Key, item.SlotType);
				if (item.AbilityDefs == null)
					continue;
				foreach (AbilityDef def in item.AbilityDefs)
					if (def != null)
						AddOwner(def.Key, item.SlotType);
			}
		}

		private void AddOwner(string key, ItemSlotType slotType)
		{
			if (!string.IsNullOrEmpty(key) && !ownerSlots.ContainsKey(key))
				ownerSlots[key] = slotType;
		}

		public bool Inherits(StatModifier modifier)
		{
			string name = modifier.Name ?? string.Empty;
			if (ExcludedNames.Contains(name) || name.StartsWith(CrystalPrefix))
				return false;
			switch (modifier.Source)
			{
				case ModifierSource.Crystal:
				case ModifierSource.PetSynergy:
				// Skill buffs belong to the unit they were cast on.
				case ModifierSource.Skill:
					return false;
				case ModifierSource.Item:
				case ModifierSource.AbilityPermanent:
				case ModifierSource.AbilityTemporary:
					if (!ownedNames.TryGetValue(name, out bool inherits))
					{
						inherits = IsWeaponOwned(name, modifier.Source);
						ownedNames[name] = inherits;
					}
					return inherits;
				default:
					return true;
			}
		}

		private bool IsWeaponOwned(string name, ModifierSource source)
		{
			// Effect names are either Def.Key or "{Def.Key}|{StatType}|{ModifierType}|{Source}".
			string key = name.Split('|')[0];
			if (ownerSlots.TryGetValue(key, out ItemSlotType slotType))
				return slotType == ItemSlotType.Weapon;
			if (warnedNames.Add(key))
				Debug.LogWarning($"Pet stats: modifier '{name}' ({source}) has no owning item, not inherited");
			return false;
		}

		public static bool SameValues(StatModifier a, StatModifier b)
		{
			return a.Name == b.Name && a.StatType == b.StatType && a.ModifierType == b.ModifierType && a.Source == b.Source &&
				a.Value == b.Value && a.Param == b.Param && a.StringParam == b.StringParam && a.Duration == b.Duration;
		}

		public static StatModifier Copy(StatModifier modifier)
		{
			return new StatModifier
			{
				Name = modifier.Name,
				StatType = modifier.StatType,
				ModifierType = modifier.ModifierType,
				Source = modifier.Source,
				Value = modifier.Value,
				Param = modifier.Param,
				StringParam = modifier.StringParam,
				Duration = modifier.Duration
			};
		}
	}
}
