using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class SoulPassAbConfig : AbTestConfigBase
	{
		public List<SoulPassAbVariant> variants = new List<SoulPassAbVariant>();

		public Dictionary<string, SoulPassAbData> ToDictionary()
		{
			return BuildVariantsDictionary<SoulPassAbData>(variants);
		}
	}
}
