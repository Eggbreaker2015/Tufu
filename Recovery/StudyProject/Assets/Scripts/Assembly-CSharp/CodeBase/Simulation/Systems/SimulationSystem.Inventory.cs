using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		public int RollFractionalCount(double value)
		{
			return 0;
		}

		[SimulationRebuild(Provenance.Null)]
		private static double GetChainedItemBaseCount(int baseItemsCount, float value)
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.Null)]
		private void UnlockSimulationWeaponSlot(int locationNumber)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void ProcessInventory()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void TryApplyArmorBaseAutoBalance(ItemModel item)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void ApplyAutoBalanceForEvolution(ItemModel item)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private int GetSpawnerItemRewardCopies(EnemySpawner spawner)
		{
			return 0;
		}

		[SimulationRebuild(Provenance.Null)]
		private float GetItemEmissionCoefficient(float timing, ItemConfig item)
		{
			return 0f;
		}

		[SimulationRebuild(Provenance.Null)]
		private bool IsPreferredItemEmissionType(ItemConfig item)
		{
			return false;
		}
	}
}
