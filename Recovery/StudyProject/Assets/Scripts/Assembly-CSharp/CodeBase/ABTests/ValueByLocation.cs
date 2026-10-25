using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class ValueByLocation
	{
		public float defaultValue = 1f;

		public LocationOverrides overridesByLocation = new LocationOverrides();
	}
}
