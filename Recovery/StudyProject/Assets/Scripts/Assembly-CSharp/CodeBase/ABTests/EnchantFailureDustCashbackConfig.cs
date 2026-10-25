using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class EnchantFailureDustCashbackConfig : AbTestConfigBase
	{
		public List<EnchantFailureDustCashbackVariant> variants = new List<EnchantFailureDustCashbackVariant>();

		public Dictionary<string, EnchantFailureDustCashbackData> ToDictionary()
		{
			return BuildVariantsDictionary<EnchantFailureDustCashbackData>(variants);
		}
	}
}
