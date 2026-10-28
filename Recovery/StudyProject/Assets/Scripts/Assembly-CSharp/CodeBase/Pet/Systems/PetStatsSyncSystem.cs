using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Pet.Systems
{
	// Rebuilds pet stats from the player's inheritable modifiers; runs after AbilityExecuteEventSystem.
	public class PetStatsSyncSystem : SystemBase
	{
		private const string HealthMultiplierName = "pet.health_mult";

		private const int StatTypeCount = 36;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PlayerConfig playerConfig;

		[Inject]
		private ItemsProvider itemsProvider;

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
				inheritance = new PetStatInheritance(itemsProvider);
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

			float healthMultiplier = pet.Definition != null ? pet.Definition.HealthMultiplier : 1f;
			bool healthMultiplierChanged = pet.SyncedHealthMultiplier != healthMultiplier;
			double previousMaxHealth = initial ? 0.0 : unit.GetStatValue(StatType.Health);
			bool changed = false;
			for (int i = 0; i < baseStats.Count; i++)
			{
				StatType type = baseStats[i].Type;
				Stat source = playerStats[(int)type];
				int version = source != null ? source.Version : 0;
				bool dirty = initial || pet.SyncedSources[i] != source || pet.SyncedVersions[i] != version ||
					(type == StatType.Health && healthMultiplierChanged);
				if (!dirty)
					continue;
				pet.SyncedSources[i] = source;
				pet.SyncedVersions[i] = version;
				// Excluded modifiers (e.g. a shield scaled by missing health) bump the version every frame; skip identical rebuilds.
				if (!initial && unit.Stats[i] != null && Matches(unit.Stats[i], source, healthMultiplier))
					continue;
				unit.Stats[i] = Build(baseStats[i], source, healthMultiplier);
				changed = true;
			}

			pet.SyncedHealthMultiplier = healthMultiplier;
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

		// True when the pet stat already holds exactly what Build would produce from this source.
		private bool Matches(Stat current, Stat source, float healthMultiplier)
		{
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
			if (current.Type == StatType.Health && healthMultiplier != 1f)
			{
				if (index >= existing.Count || existing[index].Name != HealthMultiplierName || existing[index].Value != healthMultiplier)
					return false;
				index++;
			}
			return index == existing.Count;
		}

		private Stat Build(Stat baseStat, Stat source, float healthMultiplier)
		{
			// Fresh modifier list: PlayerConfig/LastStats/Unit stats share one list instance.
			List<StatModifier> modifiers = new List<StatModifier>();
			if (source != null)
				foreach (StatModifier modifier in source.Modifiers)
					if (inheritance.Inherits(modifier))
						modifiers.Add(PetStatInheritance.Copy(modifier));
			// Max health is read from the raw value, which post-formula modifiers do not touch.
			if (baseStat.Type == StatType.Health && healthMultiplier != 1f)
				modifiers.Add(new StatModifier { Name = HealthMultiplierName, StatType = StatType.Health, ModifierType = ModifierType.PercentMultiplicative, Source = ModifierSource.Item, Value = healthMultiplier });
			return new Stat(petsModel.Lifetime, baseStat.BaseValue, baseStat.Type, modifiers);
		}
	}
}
