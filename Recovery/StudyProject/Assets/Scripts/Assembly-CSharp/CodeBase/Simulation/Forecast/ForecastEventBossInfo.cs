using System;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastEventBossInfo
	{
		public ForecastEventEnemyInfo Enemy;

		public ForecastEventRewardInfo Reward;
	}
}
