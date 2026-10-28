using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Pet.Systems
{
	// Rebuilds pet stats from the player's inheritable modifiers; runs after AbilityExecuteEventSystem.
	// Health, Damage and the weapon's typed flat damage are scaled by the pet's ratios (PetStats.GetRatios).
	public class PetStatsSyncSystem : SystemBase
	{
		private const string RatioName = "pet.ratio";

		private const int StatTypeCount = 36;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PlayerConfig playerConfig;

		[Inject]
		private PetsModel petsModel;

		private PetStatInheritance inheritance;

		private readonly Stat[] playerStats = new Stat[StatTypeCount];

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!PetConfig.Enabled || playerModel.Unit == null || petsModel.Pets.Count == 0)
				return;
			if (inheritance == null)
				inheritance = petsModel.Inheritance;
			Array.Clear(playerStats, 0, playerStats.Length);
			foreach (Stat stat in playerModel.Unit.Stats)
				if (playerStats[(int)stat.Type] == null)
					playerStats[(int)stat.Type] = stat;
			foreach (PetModel pet in petsModel.Pets)
				if (pet.Unit != null)
					Sync(pet);
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
			StatType flatType = pet.Item.Config.DamageStatTypeFlat;
			double previousMaxHealth = initial ? 0.0 : unit.GetStatValue(StatType.Health);
			bool changed = false;
			for (int i = 0; i < baseStats.Count; i++)
			{
				StatType type = baseStats[i].Type;
				Stat source = playerStats[(int)type];
				int version = source != null ? source.Version : 0;
				float ratio = GetRatio(type, flatType, ratios);
				bool dirty = initial || pet.SyncedSources[i] != source || pet.SyncedVersions[i] != version ||
					(ratiosChanged && IsRatioStat(type, flatType));
				if (!dirty)
					continue;
				pet.SyncedSources[i] = source;
				pet.SyncedVersions[i] = version;
				// Excluded modifiers (e.g. a shield scaled by missing health) bump the version every frame; skip identical rebuilds.
				if (!initial && unit.Stats[i] != null && Matches(unit.Stats[i], baseStats[i], source, ratio))
					continue;
				unit.Stats[i] = Build(baseStats[i], source, ratio);
				changed = true;
			}

			pet.SyncedRatios = ratios;
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
			return type == StatType.Health || type == StatType.Damage || type == flatType;
		}

		// Health -> health ratio; Damage and the weapon's typed flat damage (the player attack) -> attack ratio.
		private static float GetRatio(StatType type, StatType flatType, PetStatRatios ratios)
		{
			if (type == StatType.Health)
				return ratios.Health;
			if (type == StatType.Damage || type == flatType)
				return ratios.Attack;
			return 1f;
		}

		// True when the pet stat already holds exactly what Build would produce from this source.
		private bool Matches(Stat current, Stat baseStat, Stat source, float ratio)
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
			return index == existing.Count;
		}

		private Stat Build(Stat baseStat, Stat source, float ratio)
		{
			// Max health and damage are read from the raw value, which post-formula modifiers do not touch.
			StatModifier ratioModifier = ratio != 1f
				? new StatModifier { Name = RatioName, StatType = baseStat.Type, ModifierType = ModifierType.PercentMultiplicative, Source = ModifierSource.Item, Value = ratio }
				: null;
			// The player's runtime base value: in pet mode its Damage starts at PetConfig.PlayerBaseAttack.
			if (source != null)
				return PetStats.BuildInherited(source, inheritance, ratioModifier, petsModel.Lifetime);
			List<StatModifier> modifiers = new List<StatModifier>();
			if (ratioModifier != null)
				modifiers.Add(ratioModifier);
			return new Stat(petsModel.Lifetime, baseStat.BaseValue, baseStat.Type, modifiers);
		}
	}
}
