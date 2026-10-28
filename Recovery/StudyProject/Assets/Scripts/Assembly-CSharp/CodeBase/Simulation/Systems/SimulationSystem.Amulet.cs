using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		private void ProcessCrystalSimulationChunk()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void UpgradeCrystalsGreedy()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void ResetAmuletAutoBalance()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void ProcessAmuletAutoBalanceTargets(int chunkIndex)
		{
		}
	}
}
