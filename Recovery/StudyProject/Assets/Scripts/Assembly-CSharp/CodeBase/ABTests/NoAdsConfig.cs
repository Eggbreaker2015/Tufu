using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class NoAdsConfig : AbTestConfigBase
	{
		public List<NoAdsVariant> variants = new List<NoAdsVariant>();

		public Dictionary<string, NoAdsData> ToDictionary()
		{
			return BuildVariantsDictionary<NoAdsData>(variants);
		}
	}
}
