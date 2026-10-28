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

		[SimulationRebuild(Provenance.Null)]
		private void SimulateFightWithSpawner(SpawnerData bestSpawner)
		{
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
