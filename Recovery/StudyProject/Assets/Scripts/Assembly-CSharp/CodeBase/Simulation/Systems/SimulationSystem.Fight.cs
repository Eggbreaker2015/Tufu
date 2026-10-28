using Codebase.Enemy;
using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.DebugOnly)]
		private (double, double, string) GetUnlockTimingLogData(EnemySpawner spawner)
		{
			return default;
		}

		[SimulationRebuild(Provenance.Null)]
		private void ProcessDeath()
		{
		}

		// Call-site skeleton (C3b): duration, health, lethality and death are C7's; the clear goes through the
		// write point, in this body only (design 4.5).
		[SimulationRebuild(Provenance.Null, "pending:simulation.slow_fight")]
		private void SimulateFightWithSpawner(SpawnerData bestSpawner)
		{
			RebuildApplyClear(bestSpawner.spawner, 1, false);
		}

		[SimulationRebuild(Provenance.DebugOnly)]
		private double CalcDifficultSpawner(EnemySpawner spawner, float secondsRegen = 0f)
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.DebugOnly)]
		private void LogSpawnerClear(EnemySpawner spawner, double healthDelta, double fightTime, double moveTime)
		{
		}
	}
}
