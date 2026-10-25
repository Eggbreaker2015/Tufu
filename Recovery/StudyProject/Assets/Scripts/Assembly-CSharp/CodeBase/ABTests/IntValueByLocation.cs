using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class IntValueByLocation
	{
		public int defaultValue;

		public IntLocationOverrides overridesByLocation = new IntLocationOverrides();
	}
}
