using Codebase.Enemy;
using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		private void FastTick()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void AccumulateFastTickSpawnerRewards(EnemySpawner spawner, int fightsCount)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void FlushFastTickChunkRewards()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private bool SimulateFightWithSpawnerFast(SpawnerData bestSpawner, int fightsCount)
		{
			return false;
		}
	}
}
