using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class CommonRewardMultipliersData
	{
		public ValueByLocation rewardedStatValueMultiplier = new ValueByLocation();

		public ValueByLocation inAppStatValueMultiplier = new ValueByLocation();

		public float attackStatValueMultiplier = 1f;

		public float armorStatValueMultiplier = 1f;

		public float sustainStatValueMultiplier = 1f;

		public float inAppAttackStatValueMultiplier = 1f;

		public float inAppArmorStatValueMultiplier = 1f;

		public float inAppSustainStatValueMultiplier = 1f;

		public ValueByLocation soulValueMultiplier = new ValueByLocation();

		public ValueByLocation crystalDustValueMultiplier = new ValueByLocation();
	}
}
