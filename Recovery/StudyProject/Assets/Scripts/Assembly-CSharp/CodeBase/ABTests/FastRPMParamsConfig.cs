using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class FastRPMParamsConfig : AbTestConfigBase
	{
		public List<FastRPMParamsVariant> variants = new List<FastRPMParamsVariant>();

		public Dictionary<string, FastRPMParamsData> ToDictionary()
		{
			return BuildVariantsDictionary<FastRPMParamsData>(variants);
		}
	}
}
