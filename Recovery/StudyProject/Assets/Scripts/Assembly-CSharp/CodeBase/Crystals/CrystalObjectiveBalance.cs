using Codebase.Crystals.Objective;
using Codebase.Enemy;
using Codebase.Simulation;
using Codebase.Unit;
using UniMob;
using System.Linq;

namespace Codebase.Crystals
{
	public static class CrystalObjectiveBalance
	{
		public static bool UsesSnapshotScaling(int objectiveKillCount)
		{
			return objectiveKillCount > 0;
		}

		public static float GetSnapshotDay(CrystalConfig config, int objectiveKillCount)
		{
			return config.ObjectiveSnapshotBaseDay + config.ObjectiveSnapshotDayStep * (float)objectiveKillCount;
		}

		public static CrystalObjectiveUnitStats GetUnitStats(CrystalConfig config, SimulationConfig simulationConfig, EnemySpawner spawner, int objectiveKillCount)
		{
			float day = GetSnapshotDay(config, objectiveKillCount);
			double health = simulationConfig.CalculateBakedSpawnerHealth(spawner, day);
			if (objectiveKillCount > 8)
				health = System.Math.Pow(10.0, (double)objectiveKillCount) * 1000.0;
			double playerHealth = simulationConfig.SimulationData.GetByTimeChunkIndex(day).Stats.First(stat => stat.Key == StatType.Health).TotalValue;
			double fraction = System.Math.Min(1.0, (double)unchecked(objectiveKillCount + 1) / 4.0);
			double lifesteal = simulationConfig.SimulationData.GetByTimeChunkIndex(day).Stats.First(stat => stat.Key == StatType.LifestealFlat).TotalValue;
			return new CrystalObjectiveUnitStats(health, System.Math.Round(playerHealth * fraction * (double)config.ObjectiveReflectionDamageMultiplier + lifesteal));
		}

		public static void ApplyToUnit(CrystalUnit unit, Lifetime lifetime, CrystalConfig config, CrystalObjectiveUnitStats stats)
		{
			unit.ReflectionDamage = stats.ReflectionDamage;
			if (!stats.UsesSnapshotScaling)
				return;
			SetStatValue(unit, lifetime, StatType.Health, stats.Health);
			unit.CurrentHealth = stats.Health;
			unit.BuildStatLookup();
		}

		private static void SetStatValue(CrystalUnit unit, Lifetime lifetime, StatType type, double value)
		{
			for (int index = 0; index < unit.Stats.Count; index++)
				if (unit.Stats[index].Type == type)
				{
					unit.Stats[index] = new Stat(lifetime, value, type);
					return;
				}
		}
	}
}
