using System;

namespace Codebase.Simulation
{
	[Serializable]
	public struct SpawnerBakeInfo
	{
		public string Key;

		public float OptimumTime;

		public double PrevHealth;

		public double PrevDamage;

		public double Health;

		public double Damage;
	}
}
