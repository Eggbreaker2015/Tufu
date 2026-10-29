namespace Codebase.Simulation.Rebuild
{
	// Slow-phase target priority (design 4.5, P13): reward over clear time from the current position.
	public enum PriorityFamily
	{
		// Reward with every statsBenefit at 1 (the spawner's own value), times PriorityMultiplier.
		Static = 0,
		// The recovered CalculateSpawnerRewardValue (weighted by statsBenefit), or the stat's own reward when
		// GetPriority gets a stat; times PriorityMultiplier.
		Dynamic = 1
	}

	// How a slow fight advances P and T (design 4.5, P13/P14). The first two are the zero-parameter recovered
	// forms; the decomposed ones are the registered fallback (CMove, CRegen; the fight factor stays rpm.Item3).
	public enum TimeModel
	{
		// dt = farmedTime
		FarmedTime = 0,
		// dt = farmedTime - avMoveTime + GetMoveTime(currentPosition)
		FarmedTimeMoveByDistance = 1,
		// dt = CRegen * avRegenTime + (CMove * avMoveTime + rpm.Item3 * fightTime)
		Decomposed = 2,
		// as Decomposed with GetMoveTime(currentPosition) for avMoveTime
		DecomposedMoveByDistance = 3
	}

	// Which fights the slow phase refuses (design 4.5, P15).
	public enum SlowLethalityGate
	{
		// The fast phase's predicate: refuse when KillLossMultiplier * H < loss.
		SameAsFast = 0,
		// Refuse when loss >= CurrentHealth.
		CurrentHealth = 1,
		// No gate; deaths go to ProcessDeath (control).
		None = 2
	}

	// What a death costs (design 4.5, P15). Every death counts in countRespawn and resets health and position.
	public enum DeathPolicy
	{
		// The fight is lost (no clear) and the spawner is skipped for the rest of the session.
		SkipSpawner = 0,
		// The fight is lost; the spawner may be chosen again.
		Retry = 1,
		// The clear counts although the player died (the original bot had no death check).
		ClearThenRespawn = 2
	}

	// statsBenefit (design 4.5): the OnAwake constant, or the marginal kill-time gain on the next candidates.
	public enum StatBenefitModel
	{
		Constant = 0,
		MarginalKillTime = 1
	}

	// Slow-phase policy parameters (unit C7); defaults mirror Balance/sim/params/defaults/slow_policy.json.
	public partial class SimulationRebuildParams
	{
		public PriorityFamily PriorityFamily = PriorityFamily.Dynamic;
		// SmartPath: on the way to the target, clear the eligible spawners within SmartPathRadius (world units)
		// of the segment (countNeighbFight).
		public bool SmartPath = true;
		public float SmartPathRadius = 5f;
		public TimeModel TimeModel = TimeModel.FarmedTime;
		// P14: fixed at 1; free only as a registered structural change (Decomposed* time models).
		public float CMove = 1f;
		public float CRegen = 1f;
		public SlowLethalityGate SlowLethalityGate = SlowLethalityGate.SameAsFast;
		public DeathPolicy DeathPolicy = DeathPolicy.SkipSpawner;
		public StatBenefitModel StatBenefitModel = StatBenefitModel.Constant;
		// MarginalKillTime: how many not-yet-farmable spawners (OptimumTime order) the gain is measured on.
		public int StatBenefitCandidates = 5;
	}
}
