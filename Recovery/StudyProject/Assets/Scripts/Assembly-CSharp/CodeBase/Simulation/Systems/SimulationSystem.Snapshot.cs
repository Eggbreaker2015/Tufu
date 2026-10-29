using System.Collections.Generic;
using Codebase.Player.Inventory;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// Early list (config.Snapshots): every interval of active time (P06 cadence), plus the forced day snapshots
		// while !(UseFastTick && d >= FastTickStartChunkOffset). Day list (config.TimeChunkSnapshots): every forced
		// snapshot. Fields per design 4.5: G11 composition from Value and the current K2/K3; CurrentGpm is
		// dValue/dP on the early list (NaN when dP = 0, 0 for the first) and the RPM on the day list (G19).
		[SimulationRebuild(Provenance.Inferred, "simulation.make_snapshot")]
		private void MakeSnapshot(bool force = false)
		{
			float playtime = config.CurrentPlaytime;
			bool early = force
				? !(config.UseFastTick && timeChunkIndex >= config.FastTickStartChunkOffset)
				: playtime >= RebuildNextSnapshotDue();
			if (!early && !force) return;
			var ext = new SimulationExport.SnapshotExt
			{
				UnlockedWeaponSlots = playerModel.UnlockedWeaponSlots,
				UnlockedArmorSlots = playerModel.UnlockedArmorSlots,
				CurrentHealth = playerModel.Unit.CurrentHealth,
				SlowTickBasis = force ? null : SimulationExport.SlowTickBasis,
			};
			var items = new List<SimulationItemData>();
			foreach (ItemModel item in playerModel.Items)
			{
				// Level is the effective level (Copies until evolved), as in the shipped data (design 4.5).
				items.Add(new SimulationItemData { Key = item.Config.Key, Level = item.EffectiveLevel, Copies = item.Copies });
				ext.Items.Add(new SimulationExport.ItemExt { Key = item.Config.Key, Level = item.EffectiveLevel, Copies = item.Copies, Evolved = item.Evolved, EvolutionLevel = item.Evolved ? item.Level : 0 });
			}
			List<SimulationStatData> stats = RebuildSnapshotStats();
			if (early)
			{
				var list = new List<SimulationStatData>(stats.Count);
				bool first = config.Snapshots.Count == 0;
				SimulationSnapshot previous = first ? default : config.Snapshots[config.Snapshots.Count - 1];
				double dp = first ? 0.0 : (double)playtime - (double)previous.Playtime;
				foreach (SimulationStatData stat in stats)
				{
					SimulationStatData row = stat;
					if (first) row.CurrentGpm = 0.0;
					else if (dp == 0.0) row.CurrentGpm = double.NaN;
					else row.CurrentGpm = (stat.Value - RebuildSnapshotValue(previous, stat.Key)) / dp;
					list.Add(row);
				}
				config.Snapshots.Add(RebuildSnapshot(list, items));
				SimulationExport.RecordSnapshot(false, config.Snapshots.Count - 1, ext);
				RebuildLastSnapshotPlaytime = playtime;
				RebuildCount("snapshots_early");
			}
			if (force)
			{
				var list = new List<SimulationStatData>(stats.Count);
				foreach (SimulationStatData stat in stats)
				{
					SimulationStatData row = stat;
					row.CurrentGpm = RPMCalculatedValue.TryGetValue(stat.Key, out double rpm) ? rpm : 0.0;
					list.Add(row);
				}
				config.TimeChunkSnapshots.Add(RebuildSnapshot(list, items));
				SimulationExport.RecordSnapshot(true, config.TimeChunkSnapshots.Count - 1, ext);
				RebuildCount("snapshots_day");
			}
		}

		private SimulationSnapshot RebuildSnapshot(List<SimulationStatData> stats, List<SimulationItemData> items)
		{
			return new SimulationSnapshot
			{
				Playtime = config.CurrentPlaytime,
				TotalPlaytime = config.CurrentTotalPlaytime,
				Location = playerModel.LastUnlockedLocation.Number,
				TimeChunkIndex = timeChunkIndex,
				Stats = stats,
				Items = new List<SimulationItemData>(items),
				DpsData = new List<SimulationDpsData>(currentDpsData),
				DefenseData = new List<SimulationDpsData>(currentDefenseData),
			};
		}

		// Value without the simulation modifiers, composed with the K2/K3 the modifiers carry (G11, double).
		private List<SimulationStatData> RebuildSnapshotStats()
		{
			double k2 = RebuildK2;
			double k3 = RebuildK3;
			var stats = new List<SimulationStatData>(playerModel.Unit.Stats.Count);
			foreach (Stat stat in playerModel.Unit.Stats)
			{
				double value = GetSimulationSnapshotStatValue(stat);
				var row = new SimulationStatData { Key = stat.Type, Value = value, StatsFromFeaturesFlat = 0.0, PercentMultiplier = 1.0, TotalFlatValue = value, TotalValue = value };
				if (UsesInterpolationStats(stat.Type))
				{
					double totalFlat = value * (1.0 + k2);
					row.StatsFromFeaturesFlat = k2;
					row.PercentMultiplier = k3;
					row.TotalFlatValue = totalFlat;
					row.TotalValue = DirectInterpolationStats.Contains(stat.Type) ? totalFlat * k3 : totalFlat;
				}
				stats.Add(row);
			}
			return stats;
		}

		private static double RebuildSnapshotValue(SimulationSnapshot snapshot, StatType key)
		{
			foreach (SimulationStatData stat in snapshot.Stats)
				if (stat.Key == key) return stat.Value;
			return 0.0;
		}

		// P06: the interval of the last start <= P.
		private float RebuildSnapshotInterval(float playtime)
		{
			float[] starts = RebuildParams.EarlySnapshotIntervalStarts;
			float interval = RebuildParams.EarlySnapshotIntervals[0];
			for (int i = 1; i < starts.Length; i++)
				if (playtime >= starts[i]) interval = RebuildParams.EarlySnapshotIntervals[i];
			return interval;
		}

		private float RebuildNextSnapshotDue()
		{
			return (float)(RebuildLastSnapshotPlaytime + RebuildSnapshotInterval(config.CurrentPlaytime));
		}
	}
}
