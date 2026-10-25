using System;
using System.Collections.Generic;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastEventPlan
	{
		public int LeagueIndex;

		public ForecastEventEnemyInfo Golem;

		public List<ForecastEventBossInfo> Bosses;

		public ForecastEventEnemyInfo GrandBoss;

		public List<ForecastCrystalStagePlan> CrystalStages;

		public List<ForecastCrystalMilestonePlan> CrystalMilestones;
	}
}
