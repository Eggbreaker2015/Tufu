namespace Codebase.Services.Damage
{
	public struct DamageResult
	{
		public double Damage;

		public double Resist;

		public bool IsCrit;

		public bool IsSuperCrit;

		public bool IsEmpowered;

		public bool HasDodged;

		public bool HasBlocked;

		public double BlockedDamageReduction;
	}
}
