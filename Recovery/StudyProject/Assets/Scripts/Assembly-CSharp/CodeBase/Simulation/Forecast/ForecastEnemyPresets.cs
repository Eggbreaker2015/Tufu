using System;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastEnemyPresets
	{
		public ForecastEnemySettings Golem;

		public ForecastEnemySettings Boss;

		public ForecastEnemySettings GrandBoss;
	}
}
