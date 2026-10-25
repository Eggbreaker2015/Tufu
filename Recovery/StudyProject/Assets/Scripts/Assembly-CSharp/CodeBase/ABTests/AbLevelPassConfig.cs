using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class AbLevelPassConfig : AbTestConfigBase
	{
		public List<AbLevelPassVariant> variants = new List<AbLevelPassVariant>();

		public Dictionary<string, LevelPassData> ToDictionary()
		{
			return BuildVariantsDictionary<LevelPassData>(variants);
		}
	}
}
