using System;
using Codebase.Enemy;

namespace Codebase.Simulation
{
	[Serializable]
	public class SpawnerBakeArmorCandidateData
	{
		public WeaponType WeaponType;

		public float Resist;

		public double DpsByWeaponType;

		public double Score;
	}
}
