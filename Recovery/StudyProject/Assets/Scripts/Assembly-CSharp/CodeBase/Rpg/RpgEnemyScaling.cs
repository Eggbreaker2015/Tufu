using System;
using Codebase.Enemy;
using Codebase.Unit;
using UniMob;

namespace Codebase.Rpg
{
	public static class RpgEnemyScaling
	{
		public static void Apply(EnemyUnit unit, int level, Lifetime lifetime)
		{
			var c = RpgModeConfig.Instance;
			int oldLevel = unit.RpgLevel;
			double factor = Math.Pow(c.EnemyLevelGrowth, level - (oldLevel > 0 ? oldLevel : 1));
			for (int i = 0; i < unit.Stats.Count; i++)
			{
				Stat stat = unit.Stats[i];
				if (stat.Type == StatType.Health || stat.Type == StatType.Damage)
				{
					double scale = oldLevel > 0 ? 1 : stat.Type == StatType.Health ? c.EnemyHealthScale : c.EnemyDamageScale;
					unit.Stats[i] = new Stat(lifetime, stat.BaseValue * factor * scale, stat.Type, stat.Modifiers);
				}
			}
			unit.RpgLevel = level;
			unit.BuildStatLookup();
			unit.CurrentHealth = unit.GetStatValue(StatType.Health);
		}
	}
}
