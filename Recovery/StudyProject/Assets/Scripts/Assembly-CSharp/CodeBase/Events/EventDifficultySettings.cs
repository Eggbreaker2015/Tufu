using System;
using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class EventDifficultySettings
	{
		public EventDifficulty Difficulty;

		public int DurationMinutes;

		public int StagesCount;

		[NonSerialized]
		public WeaponType AttackType;

		[NonSerialized]
		public WeaponType ResistType;

		[Min(0f)]
		public int InitialMobSpawnCount;

		[SerializeReference]
		public List<RewardBase> FailedRewards;

		[SerializeReference]
		public List<RewardBase> Rewards;

		public int MinutesAhead;

		public int StepMinutes;

		public int SnapshotsPerFrame;

		public List<StatType> RewardAllowedStats;

		public ForecastAutoBalanceInput AutoBalanceInput;

		public ForecastEnemyPresets EnemyPresets;
	}
}
