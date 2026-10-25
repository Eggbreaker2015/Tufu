using System;
using Codebase.Unit;

namespace Codebase.Simulation.Forecast
{
	[Serializable]
	public struct ForecastCrystalRewardEntry
	{
		public ForecastCrystalRewardKind Kind;

		public bool CustomModifier;

		public int Tier;

		public string Key;

		public string Name;

		public int Amount;

		public StatType StatType;

		public ModifierSource Source;

		public ModifierType ModifierType;

		public float Minutes;

		public double Value;
	}
}
