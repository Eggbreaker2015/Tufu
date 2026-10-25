using System;
using System.Collections.Generic;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastCrystalStagePlan
	{
		public float Minute;

		public List<ForecastCrystalRewardEntry> Rewards;
	}
}
