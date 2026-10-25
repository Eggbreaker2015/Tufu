using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;
using System;
using System.Linq;

namespace Codebase.Simulation
{
	public class SimulationData : ScriptableObject
	{
		public List<SimulationSnapshot> Snapshots;
		public List<SimulationSnapshot> TimeChunkSnapshots;
		public SimulationSnapshot GetByPlayTime(float playTime)
		{
			return Snapshots.OrderBy(snapshot => Math.Abs(snapshot.Playtime - playTime)).FirstOrDefault();
		}

		public double GetStatValueAtTimeChunk(float timeChunkIndex, StatType statType, List<SimulationSnapshot> timeChunkSnapshots = null)
		{
			return GetByTimeChunkIndex(timeChunkIndex, timeChunkSnapshots).Stats.First(stat => stat.Key == statType).Value;
		}

		public SimulationSnapshot GetByTimeChunkIndex(float timeChunkIndex, List<SimulationSnapshot> timeChunkSnapshots = null)
		{
			var ordered = (timeChunkSnapshots ?? TimeChunkSnapshots).OrderBy(snapshot => snapshot.TimeChunkIndex).ToList();
			if (timeChunkIndex <= ordered[0].TimeChunkIndex)
				return ordered[0];
			if (!(timeChunkIndex < ordered[ordered.Count - 1].TimeChunkIndex))
				return ordered[ordered.Count - 1];
			for (int i = 1; i < ordered.Count; i++)
			{
				SimulationSnapshot left = ordered[i - 1], right = ordered[i];
				if (timeChunkIndex < left.TimeChunkIndex || timeChunkIndex > right.TimeChunkIndex)
					continue;
				return InterpolateSnapshot(left, right, (timeChunkIndex - left.TimeChunkIndex) / (right.TimeChunkIndex - left.TimeChunkIndex), timeChunkIndex);
			}

			return ordered[ordered.Count - 1];
		}

		private static SimulationSnapshot InterpolateSnapshot(SimulationSnapshot left, SimulationSnapshot right, float t, float timeChunkIndex)
		{
			var stats = left.Stats.Select(a =>
			{
				var b = right.Stats.First(value => value.Key == a.Key);
				return new SimulationStatData
				{
					Key = a.Key,
					Value = a.Value + (b.Value - a.Value) * (double)t,
					StatsFromFeaturesFlat = a.StatsFromFeaturesFlat + (b.StatsFromFeaturesFlat - a.StatsFromFeaturesFlat) * (double)t,
					PercentMultiplier = a.PercentMultiplier + (b.PercentMultiplier - a.PercentMultiplier) * (double)t,
					TotalFlatValue = a.TotalFlatValue + (b.TotalFlatValue - a.TotalFlatValue) * (double)t,
					TotalValue = a.TotalValue + (b.TotalValue - a.TotalValue) * (double)t,
					CurrentGpm = a.CurrentGpm + (b.CurrentGpm - a.CurrentGpm) * (double)t
				};
			}).ToList();
			var dps = left.DpsData.Select(a =>
			{
				var b = right.DpsData.First(value => value.WeaponType == a.WeaponType);
				return new SimulationDpsData
				{
					WeaponType = a.WeaponType,
					Value = a.Value + (b.Value - a.Value) * (double)t
				};
			}).ToList();
			var defense = left.DefenseData.Select(a =>
			{
				var b = right.DefenseData.First(value => value.WeaponType == a.WeaponType);
				return new SimulationDpsData
				{
					WeaponType = a.WeaponType,
					Value = a.Value + (b.Value - a.Value) * (double)t
				};
			}).ToList();
			return new SimulationSnapshot
			{
				Playtime = timeChunkIndex,
				TotalPlaytime = left.TotalPlaytime + (right.TotalPlaytime - left.TotalPlaytime) * t,
				Location = t >= 0.5f ? right.Location : left.Location,
				TimeChunkIndex = t >= 0.5f ? right.TimeChunkIndex : left.TimeChunkIndex,
				Stats = stats,
				Items = left.Items,
				DpsData = dps,
				DefenseData = defense
			};
		}
	}
}
