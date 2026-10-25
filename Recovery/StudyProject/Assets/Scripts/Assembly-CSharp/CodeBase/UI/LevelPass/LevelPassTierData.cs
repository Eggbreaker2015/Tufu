using Codebase.LevelPass;
using Codebase.Rewards;

namespace Codebase.UI.LevelPass
{
	public struct LevelPassTierData
	{
		public LevelPassTier Tier;

		public string PassKey;

		public int TierIndex;

		public int CurrentCount;

		public bool IsFreeRewardClaimed;

		public bool IsPaidMultipliedRewardClaimed;

		public bool IsPaidRewardClaimed;

		public bool IsPassPurchased;

		public RewardBase MultipliedFreeReward;

		public bool IsReached;

		public bool IsPreviousReached;

		public int PassIndex;

		public static bool AreEqual(LevelPassTierData a, LevelPassTierData b)
		{
			return false;
		}
	}
}
