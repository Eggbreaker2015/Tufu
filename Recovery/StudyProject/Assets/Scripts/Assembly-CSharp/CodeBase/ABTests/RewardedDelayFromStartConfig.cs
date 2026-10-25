using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class RewardedDelayFromStartConfig : AbTestConfigBase
	{
		public List<RewardedDelayFromStartVariant> variants = new List<RewardedDelayFromStartVariant>();

		public Dictionary<string, RewardedDelayFromStartData> ToDictionary()
		{
			return BuildVariantsDictionary<RewardedDelayFromStartData>(variants);
		}
	}
}
