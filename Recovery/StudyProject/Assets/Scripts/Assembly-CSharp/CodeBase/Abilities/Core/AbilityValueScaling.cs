namespace Codebase.Abilities.Core
{
	public enum AbilityValueScaling
	{
		None = 0,
		PerMissingHealthPercent = 1,
		PerHealthAbovePercent = 2,
		PerMarksOnTarget = 3,
		PerRageLevel = 4,
		PerEmptySlot = 5,
		PerConsumedShockOnTarget = 6,
		PerHitsTaken = 7,
		PerStat = 8
	}
}
