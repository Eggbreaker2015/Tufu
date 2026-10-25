using System;
using Codebase.Unit;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastEventRewardInfo
	{
		public bool HasReward;

		public StatType RewardStatType;

		public float RewardMinutes;

		public double RewardValue;
	}
}
