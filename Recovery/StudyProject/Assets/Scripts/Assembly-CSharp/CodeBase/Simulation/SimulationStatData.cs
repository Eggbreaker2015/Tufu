using System;
using Codebase.Unit;

namespace Codebase.Simulation
{
	[Serializable]
	public struct SimulationStatData
	{
		public StatType Key;

		public double Value;

		public double StatsFromFeaturesFlat;

		public double PercentMultiplier;

		public double TotalFlatValue;

		public double TotalValue;

		public double CurrentGpm;
	}
}
