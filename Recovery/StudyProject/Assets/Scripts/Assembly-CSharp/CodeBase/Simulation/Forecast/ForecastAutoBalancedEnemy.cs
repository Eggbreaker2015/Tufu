using System;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastAutoBalancedEnemy
	{
		public ForecastEnemyKind Kind;

		public float FightTimeSeconds;

		public float Difficulty;

		public double Health;

		public double Damage;
	}
}
