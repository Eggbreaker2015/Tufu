using System;
using System.Collections.Generic;
using Codebase.Simulation.Forecast;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class CrystalEventForecastSettings
	{
		[Min(1f)]
		public int DurationMinutes;

		[Min(0f)]
		public int InitialMobSpawnCount;

		public int MinutesAhead;

		public int StepMinutes;

		public int SnapshotsPerFrame;

		public List<StatType> RewardAllowedStats;

		public ForecastAutoBalanceInput AutoBalanceInput;

		public ForecastEnemyPresets EnemyPresets;
	}
}
