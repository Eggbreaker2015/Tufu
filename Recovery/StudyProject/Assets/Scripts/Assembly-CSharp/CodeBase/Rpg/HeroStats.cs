using System.Collections.Generic;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Unit;

namespace Codebase.Rpg
{
	public static class HeroStats
	{
		public static List<Stat> Build(HeroDefinition definition)
		{
			if (RpgModeConfig.Enabled) return BuildRpg();
			PlayerConfig config = GameContext.Get<PlayerConfig>();
			PetsModel pets = GameContext.Get<PetsModel>();
			List<Stat> stats = new List<Stat>(config.Stats.Count);
			foreach (Stat baseStat in config.Stats)
			{
				double baseValue = baseStat.BaseValue;
				if (baseStat.Type == StatType.Damage)
					baseValue += PetConfig.Instance.PlayerBaseAttack;
				Stat stat = new Stat(pets.Lifetime, baseValue, baseStat.Type);
				if (stat.Type == StatType.Health && definition.HealthRatio != 1f)
					Add(stat, ModifierType.PercentMultiplicative, definition.HealthRatio);
				if (stat.Type == StatType.Damage && definition.AttackRatio != 1f)
					Add(stat, ModifierType.PercentMultiplicative, definition.AttackRatio);
				if (stat.Type == StatType.UniversalDefenceFlat && definition.DefenceFlat != 0f)
					Add(stat, ModifierType.Flat, definition.DefenceFlat);
				if (stat.Type == StatType.AttackRange && definition.AttackRangeOverride != 0f)
					Add(stat, ModifierType.Flat, definition.AttackRangeOverride - (float)baseValue);
				stats.Add(stat);
			}
			return stats;
		}

		private static List<Stat> BuildRpg()
		{
			PlayerConfig config = GameContext.Get<PlayerConfig>();
			PetsModel pets = GameContext.Get<PetsModel>();
			List<Stat> result = new List<Stat>();
			foreach (Stat stat in config.Stats)
				result.Add(new Stat(pets.Lifetime, stat.BaseValue + (stat.Type == StatType.Damage ? PetConfig.Instance.PlayerBaseAttack : 0), stat.Type));
			return result;
		}

		public static void ApplyBase(UnitCore unit, HeroDefinition def) => ApplyBase(unit.Stats, def);

		public static void ApplyBase(List<Stat> stats, HeroDefinition def)
		{
			foreach (Stat stat in stats)
			{
				Remove(stat, "rpg.base");
				Remove(stat, "rpg.hero");
				if (stat.Type == StatType.Health) { AddNamed(stat, "rpg.base", ModifierType.Flat, RpgModeConfig.Instance.BaseHealthBonus); if (def != null && def.HealthRatio != 1f) Add(stat, ModifierType.PercentMultiplicative, def.HealthRatio); }
				if (stat.Type == StatType.Damage && def != null && def.AttackRatio != 1f) Add(stat, ModifierType.PercentMultiplicative, def.AttackRatio);
				if (stat.Type == StatType.UniversalDefenceFlat) { AddNamed(stat, "rpg.base", ModifierType.Flat, RpgModeConfig.Instance.BaseDefenceBonus); if (def != null && def.DefenceRatio != 1f) Add(stat, ModifierType.PercentMultiplicative, def.DefenceRatio); }
				if (stat.Type == StatType.AttackRange && def != null && def.AttackRangeOverride != 0f) Add(stat, ModifierType.Flat, def.AttackRangeOverride - (float)stat.BaseValue);
			}
		}

		public static void ApplyLevel(UnitCore unit, int level) => ApplyLevel(unit.Stats, level);

		public static void ApplyLevel(List<Stat> stats, int level)
		{
			foreach (Stat stat in stats)
			{
				Remove(stat, "rpg.level");
				if (level > 1 && (stat.Type == StatType.Health || stat.Type == StatType.Damage || stat.Type == StatType.UniversalDefenceFlat))
					AddNamed(stat, "rpg.level", ModifierType.PercentMultiplicative, (float)System.Math.Pow(RpgModeConfig.Instance.HeroLevelGrowth, level - 1));
			}
		}

		public static void ApplyStar(UnitCore unit, int star) => ApplyStar(unit.Stats, star);

		public static void ApplyStar(List<Stat> stats, int star)
		{
			foreach (Stat stat in stats)
			{
				Remove(stat, "rpg.star");
				if (stat.Type == StatType.Health || stat.Type == StatType.Damage || stat.Type == StatType.UniversalDefenceFlat)
					AddNamed(stat, "rpg.star", ModifierType.PercentMultiplicative, (float)System.Math.Pow(1f + RpgModeConfig.Instance.StarStatBonus, star - 1));
			}
		}

		private static void Remove(Stat stat, string name)
		{
			for (int i = stat.Modifiers.Count - 1; i >= 0; i--)
				if (stat.Modifiers[i].Name == name && stat.Modifiers[i].Source == ModifierSource.RpgHero) stat.RemoveModifier(stat.Modifiers[i]);
		}
		private static void AddNamed(Stat stat, string name, ModifierType type, float value)
		{
			stat.AddModifier(new StatModifier { Name = name, StatType = stat.Type, ModifierType = type, Source = ModifierSource.RpgHero, Value = value });
		}

		private static void Add(Stat stat, ModifierType type, float value)
		{
			stat.AddModifier(new StatModifier { Name = "rpg.hero", StatType = stat.Type, ModifierType = type, Source = ModifierSource.RpgHero, Value = value });
		}
	}
}
