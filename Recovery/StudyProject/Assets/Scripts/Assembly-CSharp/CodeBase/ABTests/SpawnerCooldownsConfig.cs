using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class SpawnerCooldownsConfig : AbTestConfigBase
	{
		public List<SpawnerCooldownsVariant> variants = new List<SpawnerCooldownsVariant>();

		public Dictionary<string, SpawnerCooldownsData> ToDictionary()
		{
			return BuildVariantsDictionary<SpawnerCooldownsData>(variants);
		}
	}
}
