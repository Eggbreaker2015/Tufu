using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// Disabled modules (design 4.5): off in the shipped config; switched on, the job fails instead of
		// silently running without them. Their power reaches the bot only through the K2/K3 curves.

		[SimulationRebuild(Provenance.Null, "pending:simulation.crystal_simulation")]
		private void ProcessCrystalSimulationChunk()
		{
			if (config.CrystalSimulation) RebuildFail(SimulationHarness.ExitBadRequest, "not supported: SimulationConfig.CrystalSimulation is on");
		}

		[SimulationRebuild(Provenance.Null, "pending:simulation.crystal_simulation")]
		private void UpgradeCrystalsGreedy()
		{
			if (config.CrystalSimulation) RebuildFail(SimulationHarness.ExitBadRequest, "not supported: SimulationConfig.CrystalSimulation is on");
		}

		[SimulationRebuild(Provenance.Null, "pending:simulation.amulet_auto_balance")]
		private void ResetAmuletAutoBalance()
		{
			if (config.AutoBalanceAmulet) RebuildFail(SimulationHarness.ExitBadRequest, "not supported: SimulationConfig.AutoBalanceAmulet is on");
		}

		[SimulationRebuild(Provenance.Null, "pending:simulation.amulet_auto_balance")]
		private void ProcessAmuletAutoBalanceTargets(int chunkIndex)
		{
			if (config.AutoBalanceAmulet) RebuildFail(SimulationHarness.ExitBadRequest, "not supported: SimulationConfig.AutoBalanceAmulet is on (day " + chunkIndex + ")");
		}
	}
}
