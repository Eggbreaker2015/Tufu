using System;
using System.Collections.Generic;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastCrystalMilestonePlan
	{
		public int RequiredCrystals;

		public List<ForecastCrystalRewardEntry> Rewards;
	}
}
