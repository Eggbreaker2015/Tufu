using System.Collections.Generic;
using System.Reflection;
using Codebase.Enemy;

namespace Codebase.Simulation.Rebuild
{
	// Run state of the slow-phase policy (unit C7) that no original SimulationSystem field holds. The partial
	// files of SimulationSystem carry methods only (engine state lives in .Rebuild.cs, owned by C3b), so the
	// policy keeps it here: one instance per run, recognised by the run's parameter object, which
	// PrepareSimulationRun loads anew for every run.
	internal sealed class SimulationSlowPolicyState
	{
		private static SimulationSlowPolicyState current;

		// C5's KillLossMultiplier (P01, SimulationRebuildParams.FastTick.cs) when that partial is present;
		// SameAsFast reads the same field as the fast kill gate, 1.0 (the A group's value) until then.
		private static readonly FieldInfo KillLossField = typeof(SimulationRebuildParams).GetField("KillLossMultiplier");

		private object owner;

		private SimulationRebuildParams run;

		// CalculateStatBenefit: the (session, day) of the last evaluation.
		internal int BenefitSession = -1;

		internal int BenefitDay = -1;

		// DeathPolicy.SkipSpawner: spawners lost to this session.
		internal readonly HashSet<EnemySpawner> Skipped = new HashSet<EnemySpawner>();

		internal int SkippedSession = -1;

		// The session in which the current boss's gate decision was last recorded.
		internal int GateEventSession = -1;

		internal EnemySpawner GateEventBoss;

		internal bool ProbeDone;

		internal static SimulationSlowPolicyState For(object owner, SimulationRebuildParams run)
		{
			if (current == null || !ReferenceEquals(current.owner, owner) || !ReferenceEquals(current.run, run))
				current = new SimulationSlowPolicyState { owner = owner, run = run };
			return current;
		}

		internal static double KillLossMultiplier(SimulationRebuildParams p)
		{
			if (KillLossField == null) return 1.0;
			object value = KillLossField.GetValue(p);
			return value is float f ? f : value is double d ? d : 1.0;
		}
	}

	// One slow fight as SimulationSystem.Fight.cs measured it (event slow_fight).
	internal struct SimulationSlowFight
	{
		public double Seconds, MoveSeconds, DistanceSeconds, MaxHealth, HealthBefore, HealthAfterFight, Regen;
		public float PlaytimeBefore, TotalPlaytimeBefore;
		public bool Died;
	}
}
