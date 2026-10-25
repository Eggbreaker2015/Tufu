using System;

namespace Codebase.Enemy
{
	[Serializable]
	public class IslandOptimumTimeRange
	{
		public int LocationNumber;

		public bool IsDays;

		public float MinOptimumTime;

		public float MaxOptimumTime;

		public EnemySpawner MinSpawner;

		public EnemySpawner MaxSpawner;
	}
}
