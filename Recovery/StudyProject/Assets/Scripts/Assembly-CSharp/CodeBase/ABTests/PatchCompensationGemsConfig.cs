using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class PatchCompensationGemsConfig : AbTestConfigBase
	{
		public List<PatchCompensationGemsVariant> variants = new List<PatchCompensationGemsVariant>();

		public Dictionary<string, PatchCompensationGemsData> ToDictionary()
		{
			return BuildVariantsDictionary<PatchCompensationGemsData>(variants);
		}
	}
}
