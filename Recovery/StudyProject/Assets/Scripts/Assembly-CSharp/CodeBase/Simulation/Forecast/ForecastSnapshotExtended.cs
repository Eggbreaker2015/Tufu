using System;
using System.Collections.Generic;
using Codebase.Unit;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastSnapshotExtended
	{
		public SimulationSnapshot Snapshot;

		public ForecastAutoBalanceInput AutoBalanceInput;

		public float EnemyStatsMultiplier;

		public bool HasReward;

		public StatType RewardStatType;

		public float RewardMinutes;

		public double RewardValue;

		public List<ForecastAutoBalancedEnemy> Enemies;
	}
}
