using System;
using Codebase.Enemy;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastEventEnemyInfo
	{
		public float Minute;

		public ForecastEnemyKind Kind;

		public double Health;

		public double Damage;

		public WeaponType AttackType;

		public float ResistStabbing;

		public float ResistCutting;

		public float ResistBuckshot;
	}
}
