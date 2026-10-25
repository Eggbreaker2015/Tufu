using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class RpmOldAlgorithmConfig : AbTestConfigBase
	{
		public List<RpmOldAlgorithmVariant> variants = new List<RpmOldAlgorithmVariant>();

		public Dictionary<string, RpmOldAlgorithmData> ToDictionary()
		{
			return BuildVariantsDictionary<RpmOldAlgorithmData>(variants);
		}
	}
}
