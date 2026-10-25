using Codebase.Enemy;

namespace Codebase.Meta
{
	public static class SpawnerStatDropAmountCalculator
	{
		public const int EventGolemParam = -1;

		public static double GetMultiplier(EnemySpawnerConfig config, MetaStatsModel metaStatsModel)
		{
// Original bounded ARM64 ELF VA 0x1488e3c.
            int param = config.IsEvent && config.EventDef != null && config.EventDef.UnitType == EventUnitType.Mob ? EventGolemParam : config.LocationNumber;
            return metaStatsModel.Get("stat_drop").GetValue(param);
		}
	}
}
