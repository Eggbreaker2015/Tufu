using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class NoAdsData
	{
		public bool enabled;

		public float rewardedOfferDuration = 30f;

		public float adCooldown = 30f;

		public int maxAdsPerDay;
	}
}
