using Codebase.Enemy;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// Minimal FastTick (C3b): no eligible spawner, so the day only advances. C5 adds the planning rounds
		// (CalculateSimulationData, RebuildApplyReplayCaches, CalculateSpawnerData(false), SimulateFightWithSpawnerFast,
		// RebuildFlushFastTickGates) and the active minutes passed to RebuildAdvanceFastDay.
		[SimulationRebuild(Provenance.Null, "pending:simulation.fast_tick")]
		private void FastTick()
		{
			RebuildAdvanceFastDay(0f);
		}

		[SimulationRebuild(Provenance.Null)]
		private void AccumulateFastTickSpawnerRewards(EnemySpawner spawner, int fightsCount)
		{
		}

		// Skeleton of the design 4.5 flush (stats, then items, souls, remaining gates; then empty), inert until C5's
		// AccumulateFastTickSpawnerRewards fills the fields. C5 owns and finalizes it (P18 order).
		[SimulationRebuild(Provenance.Null, "pending:simulation.fast_flush")]
		private void FlushFastTickChunkRewards()
		{
			foreach (FastTickChunkStatReward reward in fastTickChunkStatRewards.Values)
				RebuildAddStatReward(new StatModifier { Name = reward.Name, StatType = reward.StatType, ModifierType = reward.ModifierType, Source = reward.Source }, (float)reward.Value);
			foreach (var pair in fastTickChunkItemRewards)
			{
				int copies = RollFractionalCount(pair.Value);
				if (copies <= 0) continue;
				RebuildPendingItemCopies.TryGetValue(pair.Key, out int pending);
				RebuildPendingItemCopies[pair.Key] = pending + copies;
			}
			if (fastTickChunkItemRewards.Count > 0) ProcessInventory();
			foreach (var pair in fastTickChunkSoulRewards)
			{
				RebuildSoulRewards.TryGetValue(pair.Key, out double souls);
				RebuildSoulRewards[pair.Key] = souls + pair.Value;
			}
			RebuildFlushFastTickGates();
			fastTickChunkStatRewards.Clear();
			fastTickChunkItemRewards.Clear();
			fastTickChunkSoulRewards.Clear();
			fastTickChunkKillCount = 0;
		}

		// Call-site skeleton (C3b): the kill gate and the fight count are C5's; a kill goes through the write point.
		[SimulationRebuild(Provenance.Null, "pending:simulation.fast_kill_gate")]
		private bool SimulateFightWithSpawnerFast(SpawnerData bestSpawner, int fightsCount)
		{
			RebuildApplyClear(bestSpawner.spawner, fightsCount, true);
			return true;
		}
	}
}
