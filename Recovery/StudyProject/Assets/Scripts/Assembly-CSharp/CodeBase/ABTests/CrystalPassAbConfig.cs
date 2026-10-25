using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class CrystalPassAbConfig : AbTestConfigBase
	{
		public List<CrystalPassAbVariant> variants = new List<CrystalPassAbVariant>();

		public Dictionary<string, CrystalPassAbData> ToDictionary()
		{
			return BuildVariantsDictionary<CrystalPassAbData>(variants);
		}
	}
}
