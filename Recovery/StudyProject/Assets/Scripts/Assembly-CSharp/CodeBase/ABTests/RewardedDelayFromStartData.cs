using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class RewardedDelayFromStartData
	{
		public int delayMinutes = 60;

		public float delayMinutesPerUsdSpent;
	}
}
