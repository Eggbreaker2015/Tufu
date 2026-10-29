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

		private static void Add(Stat stat, ModifierType type, float value)
		{
			stat.AddModifier(new StatModifier { Name = "rpg.hero", StatType = stat.Type, ModifierType = type, Source = ModifierSource.RpgHero, Value = value });
		}
	}
}
