namespace Codebase.Crystals
{
	public readonly struct CrystalObjectiveUnitStats
	{
		public double Health { get; }
		public double ReflectionDamage { get; }
		public bool UsesSnapshotScaling { get; }
		public static CrystalObjectiveUnitStats FromSpawnerDefaults => default;

		public CrystalObjectiveUnitStats(double health, double reflectionDamage)
		{
			Health = health;
			ReflectionDamage = reflectionDamage;
			UsesSnapshotScaling = true;
		}
	}
}
