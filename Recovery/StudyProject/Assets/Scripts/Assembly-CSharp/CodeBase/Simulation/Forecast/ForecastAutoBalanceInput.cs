using System;
using System.Collections.Generic;
using Codebase.Enemy;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastAutoBalanceInput
	{
		public int ForecastLocationNumber;

		public float BossSnapshotTimingStep;

		public WeaponType EnemyAttackType;

		public float ResistStabbing;

		public float ResistCutting;

		public float ResistBuckshot;

		public float EnemyStatsGrowthPerMinute;

		public float DamageRampDelaySeconds;

		public float DamageRampPercentPerSecond;

		public float EventLastBossMinute;

		public float EventEndMinute;

		public int EventBossCount;

		public float EventBossGamma;

		public List<float> EventBossMinutes;

		public bool RandomBossResists;

		public float RewardMinutesMin;

		public float RewardMinutesMax;

		public float BossFightTimeSecondsMin;

		public float BossFightTimeSecondsMax;

		public float BossDifficultyMin;

		public float BossDifficultyMax;

		public float RewardMinutes;
	}
}
