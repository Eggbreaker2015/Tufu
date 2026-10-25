using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class OfferwallConfig : AbTestConfigBase
	{
		public List<OfferwallVariant> variants = new List<OfferwallVariant>();

		public Dictionary<string, OfferwallData> ToDictionary()
		{
			return BuildVariantsDictionary<OfferwallData>(variants);
		}
	}
}
