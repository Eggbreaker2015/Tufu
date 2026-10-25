using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class EnemyHpDamageMultData
	{
		public ValueByLocation damageMultiplier = new ValueByLocation { defaultValue = 1f };

		public double rewardedValueMultiplier = 0.5;
	}
}
