using System;
using System.Collections.Generic;
using Codebase.Abilities.Effects;
using Codebase.Balance;
using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using UniMob;

namespace Codebase.Pet
{
	// Ratios of the player's inherited stats a pet gets; Health/Attack/Defence/MoveSpeed groups.
	public struct PetStatRatios
	{
		public float Health;

		public float Attack;

		public float Defence;

		public float MoveSpeed;

		public bool Equals(PetStatRatios other)
		{
			return Health == other.Health && Attack == other.Attack && Defence == other.Defence && MoveSpeed == other.MoveSpeed;
		}
	}

	// A weapon's pet as the UI shows it; player values are the inherited set (kill-earned and direct, no equipment).
	public struct PetStatsInfo
	{
		public ItemModel Item;

		public PetDefinition Definition;

		public PetStatRatios Ratios;

		// Ratios after the next level-up (one more copy, or the next evolution level).
		public PetStatRatios NextRatios;

		// Pet level the ratios use (PetStats.GetLevel).
		public int Level;

		public double OwnAttack;

		public double NextOwnAttack;

		public double PlayerHealth;

		// Player Damage stat + the typed flat damage stat of the weapon's damage type.
		public double PlayerAttack;

		// Typed percent damage stat; multiplies the whole attack, not scaled by a ratio.
		public double AttackPercent;

		// The player's actual speed in m/s (equipment included), like PetMovementSystem.
		public double PlayerMoveSpeed;

		public float AttacksPerSecond;

		public double Health => PlayerHealth * Ratios.Health;

		public double NextHealth => PlayerHealth * NextRatios.Health;

		public double PlayerAttackShare => PlayerAttack * Ratios.Attack;

		public double Attack => OwnAttack + PlayerAttack * Ratios.Attack;

		public double NextAttack => NextOwnAttack + PlayerAttack * NextRatios.Attack;

		public double CombatMoveSpeed => PlayerMoveSpeed * Ratios.MoveSpeed;

		public double NextCombatMoveSpeed => PlayerMoveSpeed * NextRatios.MoveSpeed;
	}

	// The one place pet ratios and own attack are computed (sync, damage, movement, UI).
	public static class PetStats
	{
		private const float DefaultOwnAttackScale = 0.6f;

		private const float DefaultLevelRatioGrowth = 0.02f;

		private const int StatTypeCount = 36;

		private struct CacheEntry
		{
			public Stat Source;

			public int Version;

			public double Value;
		}

		// Inherited player values for the UI, rebuilt only when the player's stat changes.
		private static readonly CacheEntry[] inheritedCache = new CacheEntry[StatTypeCount];

		// Copies consumed by evolutions 1..level, keyed by armor / rarity / evolution level.
		private static readonly Dictionary<int, int> consumedCopiesCache = new Dictionary<int, int>();

		private static CombatBalanceConfig consumedCopiesConfig;

		[StaticReset]
		private static void ResetStaticState()
		{
			Array.Clear(inheritedCache, 0, inheritedCache.Length);
			consumedCopiesCache.Clear();
			consumedCopiesConfig = null;
		}

		// ratio = species ratio * rarity factor * (1 + growth * (level - 1)).
		public static PetStatRatios GetRatios(PetDefinition definition, Rarity rarity, int effectiveLevel)
		{
			PetConfig config = PetConfig.Instance;
			float rarityFactor = config != null ? config.GetRarityRatioFactor(rarity) : 1f;
			float growth = config != null ? config.LevelRatioGrowth : DefaultLevelRatioGrowth;
			float scale = rarityFactor * (1f + growth * (Math.Max(1, effectiveLevel) - 1));
			return new PetStatRatios
			{
				Health = (definition != null ? definition.HealthRatio : 1f) * scale,
				Attack = (definition != null ? definition.AttackRatio : 1f) * scale,
				Defence = (definition != null ? definition.DefenceRatio : 1f) * scale,
				MoveSpeed = (definition != null ? definition.MoveSpeedRatio : 1f) * scale
			};
		}

		// Item.Rarity is Divine for evolved items.
		public static PetStatRatios GetRatios(ItemModel item, PetDefinition definition)
		{
			return GetRatios(definition, item.Rarity, GetLevel(item));
		}

		// One level up: one more copy, or the next evolution (which also adds one, see GetLevel).
		public static PetStatRatios GetNextRatios(ItemModel item, PetDefinition definition)
		{
			return GetRatios(definition, item.Rarity, GetLevel(item) + 1);
		}

		// Pet level: every copy collected, so evolving (which consumes copies and resets EffectiveLevel to
		// the evolution level) keeps the level and gains the Divine factor; each evolution level adds one more.
		public static int GetLevel(ItemModel item)
		{
			if (!item.Evolved)
				return item.Copies;
			return GetConsumedCopies(item.IsArmor, item.Config.Rarity, item.Level) + item.Copies + item.Level;
		}

		private static int GetConsumedCopies(bool isArmor, Rarity rarity, int evolutionLevel)
		{
			CombatBalanceConfig balance = GameContext.Get<CombatBalanceConfig>();
			if (balance == null)
				return 0;
			if (balance != consumedCopiesConfig)
			{
				consumedCopiesCache.Clear();
				consumedCopiesConfig = balance;
			}
			int key = (evolutionLevel << 8) | ((int)rarity << 1) | (isArmor ? 1 : 0);
			if (consumedCopiesCache.TryGetValue(key, out int consumed))
				return consumed;
			consumed = 0;
			for (int level = 1; level <= evolutionLevel; level++)
				consumed += balance.GetEvolutionShardsRequirement(isArmor, rarity, level);
			consumedCopiesCache[key] = consumed;
			return consumed;
		}

		public static double GetOwnAttack(double weaponDamage, PetDefinition definition)
		{
			PetConfig config = PetConfig.Instance;
			float scale = config != null ? config.OwnAttackScale : DefaultOwnAttackScale;
			return weaponDamage * scale * (definition != null ? definition.OwnAttackMultiplier : 1f);
		}

		public static double GetOwnAttack(ItemModel item, PetDefinition definition)
		{
			return GetOwnAttack(item.Damage, definition);
		}

		// The weapon's ability attack-speed multipliers (PetAttackSystem adds the target-dependent weak-enemy factor).
		public static float GetAttackSpeedMultiplier(ItemModel item)
		{
			float attackSpeed = 1f;
			foreach (var ability in item.AbilityStates)
				attackSpeed *= ability.GetWeaponStatMultiplier(WeaponBuffType.AttackSpeed);
			// No species factor: a pet attacks at its weapon's own AttackCooldown.
			return attackSpeed * item.AbilityAttackSpeedMultiplier;
		}

		public static PetDefinition FindDefinition(ItemModel item)
		{
			PetConfig config = PetConfig.Instance;
			return item == null || config == null ? null : config.Find(item.Config);
		}

		// Pet stats of a weapon item, spawned or not; false outside pet mode or for armor.
		public static bool TryGet(ItemModel item, out PetStatsInfo info)
		{
			info = default;
			if (!PetConfig.Enabled || item == null || item.IsArmor)
				return false;
			PetDefinition definition = FindDefinition(item);
			PlayerModel playerModel = GameContext.Get<PlayerModel>();
			List<Stat> stats = playerModel.Unit?.Stats ?? playerModel.LastStats;
			if (stats == null)
				return false;
			PetStatInheritance inheritance = GameContext.Get<PetsModel>().Inheritance;
			ItemConfig config = item.Config;
			info.Item = item;
			info.Definition = definition;
			info.Ratios = GetRatios(item, definition);
			info.NextRatios = GetNextRatios(item, definition);
			info.Level = GetLevel(item);
			info.OwnAttack = GetOwnAttack(item.Damage, definition);
			info.NextOwnAttack = GetOwnAttack(item.NextLevelDamage, definition);
			info.PlayerHealth = GetInheritedValue(stats, StatType.Health, inheritance);
			info.PlayerAttack = GetInheritedValue(stats, StatType.Damage, inheritance) + GetInheritedValue(stats, config.DamageStatTypeFlat, inheritance);
			info.AttackPercent = GetInheritedValue(stats, config.DamageStatTypePercent, inheritance);
			Stat speed = FindStat(stats, StatType.MovementSpeed);
			info.PlayerMoveSpeed = speed != null ? speed.FastSpecificValue : 0.0;
			info.AttacksPerSecond = config.AttackCooldown > 0f ? GetAttackSpeedMultiplier(item) / config.AttackCooldown : 0f;
			return true;
		}

		// Value the pet's copy of this player stat has before ratios (PetStatsSyncSystem.Build without the ratio modifier).
		public static double GetInheritedValue(List<Stat> stats, StatType type, PetStatInheritance inheritance)
		{
			Stat source = FindStat(stats, type);
			if (source == null)
				return 0.0;
			int index = (int)type;
			if (index < 0 || index >= inheritedCache.Length)
				return BuildInherited(source, inheritance, null, Lifetime.Eternal).FastValue;
			CacheEntry entry = inheritedCache[index];
			if (entry.Source == source && entry.Version == source.Version)
				return entry.Value;
			entry.Source = source;
			entry.Version = source.Version;
			entry.Value = BuildInherited(source, inheritance, null, Lifetime.Eternal).FastValue;
			inheritedCache[index] = entry;
			return entry.Value;
		}

		// Copy of a player stat with only the inherited modifiers plus an optional ratio modifier.
		public static Stat BuildInherited(Stat source, PetStatInheritance inheritance, StatModifier extra, Lifetime lifetime)
		{
			// Fresh modifier list: PlayerConfig/LastStats/Unit stats share one list instance.
			List<StatModifier> modifiers = new List<StatModifier>();
			foreach (StatModifier modifier in source.Modifiers)
				if (inheritance.Inherits(modifier))
					modifiers.Add(PetStatInheritance.Copy(modifier));
			if (extra != null)
				modifiers.Add(extra);
			return new Stat(lifetime, source.BaseValue, source.Type, modifiers);
		}

		public static Stat FindStat(List<Stat> stats, StatType type)
		{
			foreach (Stat stat in stats)
				if (stat != null && stat.Type == type)
					return stat;
			return null;
		}
	}
}
