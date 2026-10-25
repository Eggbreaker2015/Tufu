using System;
using System.Collections.Generic;

namespace Codebase.ABTests
{
	[Serializable]
	public class EnemyHpDamageMultConfig : AbTestConfigBase
	{
		public List<EnemyHpDamageMultVariant> variants = new List<EnemyHpDamageMultVariant>();

		public Dictionary<string, EnemyHpDamageMultData> ToDictionary()
		{
			return BuildVariantsDictionary<EnemyHpDamageMultData>(variants);
		}
	}
}
