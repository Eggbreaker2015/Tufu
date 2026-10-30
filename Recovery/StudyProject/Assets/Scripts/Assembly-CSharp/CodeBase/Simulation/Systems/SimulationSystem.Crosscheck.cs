using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Simulation.Harness;
using Codebase.Unit;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// ---- DP8 (design 3.4; unit P11): spawnerData of an overridden state, read by tufu_balance.crosscheck.rpm ----
		// Called by RebuildApplyDayOverride (.Replay.cs) with the day's override record when the job's output
		// crosscheck_dp8 is on. Adds "crosscheck_dp8" to the record:
		//   gpm        spawnerData as the override left it (CalculateSpawnerData(true): K2/K3(d) modifiers, the day's caches)
		//   rpm_first  the same day under RpmFirst (G20): K2/K3 modifiers of day d-1, the stored caches of day d, K4/K5
		//              at d; its spawnerData and the RPM of a running maximum over the overridden days of this job only
		// The engine state is restored afterwards (RPMCalculatedValue, K(d) modifiers, caches, spawnerData), so the
		// day's digest and the rest of the run are those of a job without the output.
		private void RebuildCrosscheckDay(JObject record)
		{
			SimulationRunRequest.Job job = SimulationHarness.CurrentJob;
			if (!SimulationCrosscheck.Enabled(job)) return;
			int day = timeChunkIndex;
			var data = new JObject
			{
				["day"] = day,
				["k2"] = SimulationRuntimeProbes.F(RebuildK2),
				["k3"] = SimulationRuntimeProbes.F(RebuildK3),
				["current_health"] = SimulationRuntimeProbes.D(playerModel.Unit.CurrentHealth),
				["gpm"] = RebuildCrosscheckSpawners(),
			};
			List<KeyValuePair<StatType, double>> saved = RPMCalculatedValue.ToList();
			int lagDay = Math.Max(day - 1, 0);
			timeChunkIndex = lagDay;
			UpdateInterpolationStatModifier();
			timeChunkIndex = day;
			float lagK2 = RebuildK2, lagK3 = RebuildK3;
			CalculateSimulationData();
			RebuildApplyReplayCaches();
			Dictionary<StatType, double> chain = SimulationCrosscheck.Chain(job);
			foreach (StatType type in Enum.GetValues(typeof(StatType)))
				RPMCalculatedValue[type] = chain.TryGetValue(type, out double v) ? v : 0.0;
			foreach (StatType type in Enum.GetValues(typeof(StatType))) CalculateRPMValueTimeChunk(type);
			foreach (StatType type in Enum.GetValues(typeof(StatType))) chain[type] = RPMCalculatedValue[type];
			data["rpm_first"] = new JObject
			{
				["modifier_day"] = lagDay,
				["k2"] = SimulationRuntimeProbes.F(lagK2),
				["k3"] = SimulationRuntimeProbes.F(lagK3),
				["stats"] = new JArray(playerModel.Unit.Stats.Select(s => (object)new JArray((int)s.Type, SimulationRuntimeProbes.D(GetForecastStatValue(s.Type)))).ToArray()),
				["dps_cache"] = RebuildCrosscheckPairs(dpsDataCache.OrderBy(x => x.Key).Select(x => x.Value)),
				["defense_cache"] = RebuildCrosscheckPairs(defenseDataCache.OrderBy(x => x.Key).Select(x => x.Value)),
				["spawners"] = RebuildCrosscheckSpawners(),
				["rpm"] = new JArray(chain.OrderBy(x => x.Key).Select(x => (object)new JArray((int)x.Key, SimulationRuntimeProbes.D(x.Value))).ToArray()),
			};
			foreach (KeyValuePair<StatType, double> pair in saved) RPMCalculatedValue[pair.Key] = pair.Value;
			UpdateInterpolationStatModifier();
			CalculateSimulationData();
			RebuildApplyReplayCaches();
			record["crosscheck_dp8"] = data;
		}

		// spawnerData in its order: [runtime id, the nine doubles of SpawnerData as one hex string].
		private JArray RebuildCrosscheckSpawners()
		{
			var rows = new JArray();
			foreach (KeyValuePair<Codebase.Enemy.EnemySpawner, SpawnerData> pair in spawnerData)
			{
				SpawnerData d = pair.Value;
				rows.Add(new JArray(RebuildSpawnerId(pair.Key), string.Concat(
					SimulationRuntimeProbes.D(d.healthDelta), SimulationRuntimeProbes.D(d.fightTime), SimulationRuntimeProbes.D(d.playerDPS),
					SimulationRuntimeProbes.D(d.enemyDPS), SimulationRuntimeProbes.D(d.rewardValue), SimulationRuntimeProbes.D(d.playerRPS),
					SimulationRuntimeProbes.D(d.farmedTime), SimulationRuntimeProbes.D(d.avMoveTime), SimulationRuntimeProbes.D(d.avRegenTime))));
			}
			return rows;
		}

		private static JArray RebuildCrosscheckPairs(IEnumerable<SimulationDpsData> data) =>
			new JArray(data.Select(x => (object)new JArray((int)x.WeaponType, SimulationRuntimeProbes.D(x.Value))).ToArray());
	}
}
