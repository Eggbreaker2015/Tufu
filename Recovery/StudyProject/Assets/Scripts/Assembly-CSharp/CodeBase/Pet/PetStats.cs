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

		// Ratios after the next pet level-up.
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

		public double PlayerMoveSpeed;

		public float AttacksPerSecond;

		// Equipped weapon slots of this species' damage type (this one included when equipped) and the synergy tier it reaches.
		public int SynergyCount;

		public PetSynergyTier Synergy;

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
		public const int MaxSkillLevel = 10;

		private const float DefaultOwnAttackScale = 0.6f;

		private const float OldLevelRatioGrowth = 0.02f;

		private const float DefaultLevelGrowthScale = 0.002f;

		private const float DefaultLevelGrowthExponent = 2.5f;

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

		// Combat move speed uses the species ratio alone.
		public static PetStatRatios GetRatios(PetDefinition definition, Rarity rarity, int effectiveLevel)
		{
			PetConfig config = PetConfig.Instance;
			float rarityFactor = config != null ? config.GetRarityRatioFactor(rarity) : 1f;
			float scale = rarityFactor * (float)GetLevelScale(effectiveLevel);
			return new PetStatRatios
			{
				Health = (definition != null ? definition.HealthRatio : 1f) * scale * (config != null ? config.HealthScale : 0.5f),
				Attack = (definition != null ? definition.AttackRatio : 1f) * scale,
				Defence = (definition != null ? definition.DefenceRatio : 1f) * scale,
				MoveSpeed = definition != null ? definition.MoveSpeedRatio : 1f
			};
		}

		// Item.Rarity is Divine for evolved items.
		public static PetStatRatios GetRatios(ItemModel item, PetDefinition definition)
		{
			return GetRatios(definition, item.Rarity, GetLevel(item));
		}

		// Ratios at the next pet level.
		public static PetStatRatios GetNextRatios(ItemModel item, PetDefinition definition)
		{
			return GetRatios(definition, item.Rarity, GetLevel(item) + 1);
		}

		// Progress preserves copies consumed by evolution and adds each evolution level.
		public static int GetLevelProgress(ItemModel item)
		{
			if (!item.Evolved)
				return item.Copies;
			return GetConsumedCopies(item.IsArmor, item.Config.Rarity, item.Level) + item.Copies + item.Level;
		}

		public static int LevelFromProgress(int progress)
		{
			long value = Math.Max(1, progress) - 1L;
			int level = (int)((1.0 + Math.Sqrt(1.0 + 8.0 * value)) / 2.0);
			while ((long)level * (level - 1) / 2 > value) level--;
			while ((long)level * (level + 1) / 2 <= value) level++;
			return level;
		}

		public static int GetLevel(ItemModel item) => LevelFromProgress(GetLevelProgress(item));

		public static int ProgressInLevel(int progress, int level)
		{
			return (int)(Math.Max(1, progress) - 1L - (long)level * (level - 1) / 2);
		}

		public static int CopiesToNextLevel(int progress, int level)
		{
			return level - ProgressInLevel(progress, level);
		}

		private static double GetLevelScale(int level)
		{
			PetConfig config = PetConfig.Instance;
			double growth = config != null ? config.LevelGrowthScale : DefaultLevelGrowthScale;
			double exponent = config != null ? config.LevelGrowthExponent : DefaultLevelGrowthExponent;
			return 1.0 + growth * Math.Pow(Math.Max(1, level) - 1, exponent);
		}

		// Phase3Spec section 2: 1 + floor((item.Level - 1) / 3), evolved +2, capped at 10. item.Level is the
		// evolution level (1 before the first evolution, then 1..MaxEvolutionLevel). Unevolved pets stay at 1; evolution levels
		// 1-3/4-6/7-9/10-12/13-15 give 3/4/5/6/7.
		public static int GetSkillLevel(ItemModel item)
		{
			if (item == null)
				return 1;
			int itemLevel = Math.Max(1, item.Level);
			int level = 1 + (itemLevel - 1) / 3;
			if (item.Evolved)
				level += 2;
			return Math.Min(MaxSkillLevel, Math.Max(1, level));
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

		public static double GetOwnAttack(double weaponDamage, PetDefinition definition, int progress)
		{
			PetConfig config = PetConfig.Instance;
			float scale = config != null ? config.OwnAttackScale : DefaultOwnAttackScale;
			int value = Math.Max(1, progress);
			double adjustment = GetLevelScale(LevelFromProgress(value)) / (1.0 + OldLevelRatioGrowth * (value - 1.0));
			adjustment = Math.Max(0.5, Math.Min(1.2, adjustment));
			return weaponDamage * scale * (definition != null ? definition.OwnAttackMultiplier : 1f) * adjustment;
		}


		public static double GetOwnAttack(ItemModel item, PetDefinition definition)
		{
			return GetOwnAttack(item.Damage, definition, GetLevelProgress(item));
		}

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
			if (item == null || item.IsArmor)
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
			int progress = GetLevelProgress(item);
			int needed = CopiesToNextLevel(progress, info.Level);
			info.OwnAttack = GetOwnAttack(item.Damage, definition, progress);
			info.NextOwnAttack = GetOwnAttack(item.Damage + item.StatPerCopy * needed, definition, progress + needed);
			info.PlayerHealth = GetInheritedValue(stats, StatType.Health, inheritance);
			info.PlayerAttack = GetInheritedValue(stats, StatType.Damage, inheritance) + GetInheritedValue(stats, config.DamageStatTypeFlat, inheritance);
			info.AttackPercent = GetInheritedValue(stats, config.DamageStatTypePercent, inheritance);
			Stat speed = FindStat(stats, StatType.MovementSpeed);
			info.PlayerMoveSpeed = speed != null ? speed.FastSpecificValue : 0.0;
			// Ability tick factors are only kept up to date for equipped items (AbilityExecuteEventSystem); others read 0 or stale.
			float attackSpeed = playerModel.EquippedItems.Contains(item) ? GetAttackSpeedMultiplier(item) : 1f;
			info.AttacksPerSecond = config.AttackCooldown > 0f ? attackSpeed / config.AttackCooldown : 0f;
			info.SynergyCount = PetCollection.CountEquippedOfType(PetCollection.GetDamageType(item, PetConfig.Instance));
			info.Synergy = PetCollection.GetSynergyTier(info.SynergyCount);
			return true;
		}

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

		// Copy of a player stat with only the inherited modifiers plus the pet's own extras (ratio, synergy), in order.
		public static Stat BuildInherited(Stat source, PetStatInheritance inheritance, IReadOnlyList<StatModifier> extras, Lifetime lifetime)
		{
			// Fresh modifier list: PlayerConfig/LastStats/Unit stats share one list instance.
			List<StatModifier> modifiers = new List<StatModifier>();
			foreach (StatModifier modifier in source.Modifiers)
				if (inheritance.Inherits(modifier))
					modifiers.Add(PetStatInheritance.Copy(modifier));
			if (extras != null)
				for (int i = 0; i < extras.Count; i++)
					modifiers.Add(extras[i]);
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
