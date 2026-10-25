using System;
using System.Collections.Generic;

namespace Codebase.Simulation
{
	[Serializable]
	public struct SimulationSnapshot
	{
		public float Playtime;

		public float TotalPlaytime;

		public int Location;

		public int TimeChunkIndex;

		public List<SimulationStatData> Stats;

		public List<SimulationItemData> Items;

		public List<SimulationDpsData> DpsData;

		public List<SimulationDpsData> DefenseData;
	}
}
