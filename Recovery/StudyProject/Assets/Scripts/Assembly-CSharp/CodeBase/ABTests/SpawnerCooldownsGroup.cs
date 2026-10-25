using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class SpawnerCooldownsGroup
	{
		public ValueByLocation cooldownMinutes = new ValueByLocation();

		public IntValueByLocation charges = new IntValueByLocation();
	}
}
