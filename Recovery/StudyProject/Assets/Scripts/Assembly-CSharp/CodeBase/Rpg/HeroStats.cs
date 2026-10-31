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
			HeroRosterModel roster = GameContext.Get<HeroRosterModel>();
			List<Stat> stats = new List<Stat>();
			for (int i = 0; i <= (int)StatType.AttackSpeed; i++)
			{
				StatType type = (StatType)i;
				if (type == StatType.MagicDamageFlat || type == StatType.MagicDamagePercent || type == StatType.MagicDefenceFlat || type == StatType.MagicDefencePercent || type == StatType.LifestealPercent || type == StatType.VisualScaleMultiplier) continue;
				double value = 0;
				switch (type)
				{
					case StatType.Health: value = 120; break;
					case StatType.Damage: value = 10; break;
					case StatType.UniversalDefenceFlat: value = 2; break;
					case StatType.HealthRegen: value = 0.5; break;
					case StatType.AttackRange: value = 3.4; break;
					case StatType.MovementSpeed:
					case StatType.StabDefencePercent:
					case StatType.CutDefencePercent:
					case StatType.BuckshotDefencePercent:
					case StatType.StabDamagePercent:
					case StatType.CutDamagePercent:
					case StatType.BuckshotDamagePercent:
					case StatType.CritChance:
					case StatType.CritDamagePercent:
					case StatType.SuperCritDamagePercent:
					case StatType.BlockAmount:
					case StatType.AttackSpeed: value = 1; break;
				}
				stats.Add(new Stat(roster.Lifetime, value, type));
			}
			return stats;
		}

		public static void ApplyBase(UnitCore unit, HeroDefinition def) => ApplyBase(unit.Stats, def);

		public static void ApplyBase(List<Stat> stats, HeroDefinition def)
		{
			foreach (Stat stat in stats)
			{
				Remove(stat, "rpg.base");
				Remove(stat, "rpg.hero");
				if (stat.Type == StatType.Health) { if (def != null && def.HealthRatio != 1f) Add(stat, ModifierType.PercentMultiplicative, def.HealthRatio); }
				if (stat.Type == StatType.Damage && def != null && def.AttackRatio != 1f) Add(stat, ModifierType.PercentMultiplicative, def.AttackRatio);
				if (stat.Type == StatType.UniversalDefenceFlat) { if (def != null && def.DefenceRatio != 1f) Add(stat, ModifierType.PercentMultiplicative, def.DefenceRatio); }
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
