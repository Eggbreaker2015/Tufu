using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Shop;
using UniMob;
using UniMob.Core;

namespace Codebase.LevelPass
{
	public class LevelPassModel : Model<LevelPassUd>
	{
		[Inject]
		private LevelPassConfig levelPassConfig;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private ChestQueueModel chestQueue;

		private ComputedAtom<bool> __HasUnclaimedRewards;

		public List<LevelPassDef> Passes => null;

		public bool HasUnclaimedRewards => false;

		public LevelPassModel(Lifetime lifetime)
			: base(default(Lifetime))
		{
		}

		public override void Initialize()
		{
		}

		public int GetTierProgress(string passKey, int tierIndex)
		{
			return 0;
		}

		public bool IsTierCompleted(string passKey, int tierIndex)
		{
			return false;
		}

		public bool IsFreeRewardClaimed(string passKey, int tierIndex)
		{
			return false;
		}

		public bool IsPaidMultipliedRewardClaimed(string passKey, int tierIndex)
		{
			return false;
		}

		public bool IsPaidRewardClaimed(string passKey, int tierIndex)
		{
			return false;
		}

		public bool IsPassPurchased(LevelPassDef def)
		{
			return false;
		}

		public void MarkPassPurchased(string storeId)
		{
		}

		public void ClaimFreeReward(string passKey, int tierIndex)
		{
		}

		public RewardBase CreateMultipliedFreeReward(LevelPassTier tier)
		{
			return null;
		}

		public void ClaimPaidMultipliedReward(string passKey, int tierIndex)
		{
		}

		public void ClaimPaidReward(string passKey, int tierIndex)
		{
		}

		public void CheatCompleteCurrentTiers()
		{
		}

		public int GetCurrentTierIndex(LevelPassDef pass)
		{
			return 0;
		}

		private void EnsurePassDataExists()
		{
		}

		private LevelPassDataUd GetPassData(string passKey)
		{
			return null;
		}

		private LevelPassDataUd GetOrCreatePassData(string passKey)
		{
			return null;
		}

		private LevelPassTierDataUd GetTierData(string passKey, int tierIndex)
		{
			return null;
		}

		private void OnEnemyDied(EnemyUnit enemy)
		{
		}

		private int AutoCompleteSkippedTiersDirect(LevelPassDef pass, LevelPassDataUd passData)
		{
			return 0;
		}

		private void SendTierAnalyticsDirect(LevelPassDef pass, LevelPassDataUd passData, int highestCompletedBefore, int highestCompletedAfter)
		{
		}

		private bool IsTierCompletedDirect(LevelPassDef pass, LevelPassDataUd passData, int tierIndex)
		{
			return false;
		}

		private bool CheckHasUnclaimedRewards()
		{
			return false;
		}

		private LevelPassDef FindPass(string passKey)
		{
			return null;
		}

		private static RewardBase MultiplyReward(RewardBase reward, int multiplier)
		{
			return null;
		}
	}
}
