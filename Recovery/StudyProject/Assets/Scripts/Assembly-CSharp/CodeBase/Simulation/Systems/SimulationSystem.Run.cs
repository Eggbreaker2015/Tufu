using Codebase.Balance;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		private void ExecuteTick()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void PrepareSimulationRun()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void CheckSimulationStopConditions()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void Tick()
		{
		}

		[SimulationRebuild(Provenance.DebugOnly)]
		public float GetNextLocationInterpolationTiming(int locationNumber)
		{
			return 0f;
		}

		[SimulationRebuild(Provenance.Null)]
		public float ConvertIslandTimingToDays(float islandTiming)
		{
			return 0f;
		}

		[SimulationRebuild(Provenance.Null)]
		private void ResetCooldowns()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void UpdateSpawnerChargesChunk()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void RecalculateChunkRpmValues()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private int GetSpawnerChargesPerClear(EnemySpawner spawner)
		{
			return 0;
		}

		[SimulationRebuild(Provenance.Null)]
		public int GetForecastUnlockedLocationNumber()
		{
			return 0;
		}

		[SimulationRebuild(Provenance.Null)]
		public float GetSimulationInterpolationCoefficient(float timing, BalanceInterpolationType type, Rarity rarity = Rarity.Rare)
		{
			return 0f;
		}

		[SimulationRebuild(Provenance.Null)]
		private void UpdateInterpolationStatModifier()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private double GetSimulationSnapshotStatValue(Stat stat)
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.Null)]
		private bool UsesInterpolationStats(StatType statType)
		{
			return false;
		}
	}
}
