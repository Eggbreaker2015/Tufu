using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class CommonRewardMultipliersConfig : AbTestConfigBase
	{
		public List<CommonRewardMultipliersVariant> variants = new List<CommonRewardMultipliersVariant>();

		public Dictionary<string, CommonRewardMultipliersData> ToDictionary()
		{
			return BuildVariantsDictionary<CommonRewardMultipliersData>(variants);
		}
	}
}
