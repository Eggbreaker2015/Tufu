using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Pet.Systems
{
	// Rebuilds pet stats from the player's inheritable modifiers; runs after AbilityExecuteEventSystem.
	// Health, Damage and the weapon's typed flat damage are scaled by the pet's ratios (PetStats.GetRatios).
	// Flat regen and flat lifesteal follow the health ratio, so a pet heals the same share of its max health as the player.
	// Pet-only synergy modifiers (PetConfig.SynergyTiers, by equipped pets of the same damage type) follow the ratio modifier.
	public class PetStatsSyncSystem : SystemBase
	{
		private const string RatioName = "pet.ratio";

		private const int StatTypeCount = 36;

		private const int WeaponTypeCount = 5;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PlayerConfig playerConfig;

		[Inject]
		private PetsModel petsModel;

		private PetStatInheritance inheritance;

		private readonly Stat[] playerStats = new Stat[StatTypeCount];

		// Equipped weapon slots per species damage type, dead pets included (PetCollection.CountEquippedOfType).
		private readonly int[] equippedByType = new int[WeaponTypeCount];

		private readonly List<StatModifier> extras = new List<StatModifier>();

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;
			if (!PetConfig.Enabled || playerModel.Unit == null || petsModel.Pets.Count == 0)
				return;
			if (inheritance == null)
				inheritance = petsModel.Inheritance;
			Array.Clear(playerStats, 0, playerStats.Length);
			foreach (Stat stat in playerModel.Unit.Stats)
				if (playerStats[(int)stat.Type] == null)
					playerStats[(int)stat.Type] = stat;
			CountEquippedByType();
			foreach (PetModel pet in petsModel.Pets)
				if (pet.Unit != null)
					Sync(pet);
		}

		private void CountEquippedByType()
		{
			Array.Clear(equippedByType, 0, equippedByType.Length);
			PetConfig config = PetConfig.Instance;
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
			{
				if (slot.SlotType != ItemSlotType.Weapon || !slot.IsEquipped)
					continue;
				int type = (int)PetCollection.GetDamageType(slot.ItemModel, config);
				if (type >= 0 && type < equippedByType.Length)
					equippedByType[type]++;
			}
		}

		private PetSynergyTier GetSynergy(PetModel pet, out int key)
		{
			WeaponType type = pet.Definition != null ? pet.Definition.DamageType : pet.Item.Config.DamageType;
			int index = (int)type;
			PetSynergyTier tier = index >= 0 && index < equippedByType.Length ? PetCollection.GetSynergyTier(equippedByType[index]) : null;
			int tierIndex = PetCollection.GetSynergyTierIndex(tier);
			key = tier != null ? (index << 8) | (tierIndex & 0xFF) : -1;
			return tier;
		}

		private void Sync(PetModel pet)
		{
			List<Stat> baseStats = playerConfig.Stats;
			PetUnit unit = pet.Unit;
			bool initial = pet.SyncedVersions == null || pet.SyncedVersions.Length != baseStats.Count;
			if (initial)
			{
				pet.SyncedSources = new Stat[baseStats.Count];
				pet.SyncedVersions = new int[baseStats.Count];
				unit.Stats = new List<Stat>(baseStats.Count);
				for (int i = 0; i < baseStats.Count; i++)
					unit.Stats.Add(null);
			}

			// Level-ups, copies and evolution change the ratios without touching the player's stats.
			PetStatRatios ratios = PetStats.GetRatios(pet.Item, pet.Definition);
			bool ratiosChanged = !pet.SyncedRatios.Equals(ratios);
			PetSynergyTier synergy = GetSynergy(pet, out int synergyKey);
			// Matches below skips the stats the tier change leaves identical.
			bool synergyChanged = pet.SyncedSynergyKey != synergyKey;
			StatType flatType = pet.Item.Config.DamageStatTypeFlat;
			double previousMaxHealth = initial ? 0.0 : unit.GetStatValue(StatType.Health);
			bool changed = false;
			for (int i = 0; i < baseStats.Count; i++)
			{
				StatType type = baseStats[i].Type;
				Stat source = playerStats[(int)type];
				int version = source != null ? source.Version : 0;
				float ratio = GetRatio(type, flatType, ratios);
				bool dirty = initial || synergyChanged || pet.SyncedSources[i] != source || pet.SyncedVersions[i] != version ||
					(ratiosChanged && IsRatioStat(type, flatType));
				if (!dirty)
					continue;
				pet.SyncedSources[i] = source;
				pet.SyncedVersions[i] = version;
				// Excluded modifiers (e.g. a shield scaled by missing health) bump the version every frame; skip identical rebuilds.
				if (!initial && unit.Stats[i] != null && Matches(unit.Stats[i], baseStats[i], source, ratio, synergy))
					continue;
				FillExtras(type, ratio, synergy);
				unit.Stats[i] = Build(baseStats[i], source);
				changed = true;
			}

			pet.SyncedRatios = ratios;
			pet.SyncedSynergyKey = synergyKey;
			if (!changed)
				return;
			unit.BuildStatLookup();
			double maxHealth = unit.GetStatValue(StatType.Health);
			// A pet respawned dead (RestoreDeath) stays at zero until it is revived.
			if (initial)
			{
				if (pet.State == PetState.Alive)
					unit.CurrentHealth = maxHealth;
			}
			else if (pet.State == PetState.Alive)
			{
				if (maxHealth > previousMaxHealth)
					unit.CurrentHealth += maxHealth - previousMaxHealth;
				unit.CurrentHealth = Math.Min(unit.CurrentHealth, maxHealth);
			}
		}

		private static bool IsRatioStat(StatType type, StatType flatType)
		{
			return IsHealthRatioStat(type) || type == StatType.Damage || type == flatType;
		}

		private static bool IsHealthRatioStat(StatType type)
		{
			return type == StatType.Health || type == StatType.HealthRegen || type == StatType.LifestealFlat;
		}

		// Health, regen and flat lifesteal -> health ratio; Damage and the weapon's typed flat damage (the player attack) -> attack ratio.
		private static float GetRatio(StatType type, StatType flatType, PetStatRatios ratios)
		{
			if (IsHealthRatioStat(type))
				return ratios.Health;
			if (type == StatType.Damage || type == flatType)
				return ratios.Attack;
			return 1f;
		}

		// The pet's own modifiers of one stat, after the inherited ones: the ratio, then the synergy tier's entries for this stat.
		private void FillExtras(StatType type, float ratio, PetSynergyTier synergy)
		{
			extras.Clear();
			// Max health and damage are read from the raw value, which post-formula modifiers do not touch.
			if (ratio != 1f)
				extras.Add(new StatModifier { Name = RatioName, StatType = type, ModifierType = ModifierType.PercentMultiplicative, Source = ModifierSource.Item, Value = ratio });
			if (synergy == null || synergy.Modifiers == null)
				return;
			for (int i = 0; i < synergy.Modifiers.Count; i++)
			{
				StatModifier template = synergy.Modifiers[i];
				if (template == null || template.StatType != type)
					continue;
				StatModifier modifier = PetStatInheritance.Copy(template);
				modifier.Source = ModifierSource.PetSynergy;
				modifier.Name = GetSynergyName(template, i);
				extras.Add(modifier);
			}
		}

		private static string GetSynergyName(StatModifier template, int index)
		{
			return string.IsNullOrEmpty(template.Name) ? "pet_synergy." + index : template.Name;
		}

		// True when the pet stat already holds exactly what Build would produce (inherited, ratio, synergy); allocation-free.
		private bool Matches(Stat current, Stat baseStat, Stat source, float ratio, PetSynergyTier synergy)
		{
			if (current.BaseValue != (source != null ? source.BaseValue : baseStat.BaseValue))
				return false;
			List<StatModifier> existing = current.Modifiers;
			int index = 0;
			if (source != null)
				foreach (StatModifier modifier in source.Modifiers)
				{
					if (!inheritance.Inherits(modifier))
						continue;
					if (index >= existing.Count || !PetStatInheritance.SameValues(existing[index], modifier))
						return false;
					index++;
				}
			if (ratio != 1f)
			{
				if (index >= existing.Count || existing[index].Name != RatioName || existing[index].Value != ratio)
					return false;
				index++;
			}
			if (synergy != null && synergy.Modifiers != null)
				for (int i = 0; i < synergy.Modifiers.Count; i++)
				{
					StatModifier template = synergy.Modifiers[i];
					if (template == null || template.StatType != baseStat.Type)
						continue;
					if (index >= existing.Count)
						return false;
					StatModifier modifier = existing[index];
					if (modifier.Source != ModifierSource.PetSynergy || modifier.Name != GetSynergyName(template, i) || modifier.StatType != template.StatType ||
						modifier.ModifierType != template.ModifierType || modifier.Value != template.Value || modifier.Param != template.Param ||
						modifier.StringParam != template.StringParam)
						return false;
					index++;
				}
			return index == existing.Count;
		}

		private Stat Build(Stat baseStat, Stat source)
		{
			// The player's runtime base value: in pet mode its Damage starts at PetConfig.PlayerBaseAttack.
			if (source != null)
				return PetStats.BuildInherited(source, inheritance, extras, petsModel.Lifetime);
			List<StatModifier> modifiers = new List<StatModifier>(extras);
			return new Stat(petsModel.Lifetime, baseStat.BaseValue, baseStat.Type, modifiers);
		}
	}
}
