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

		public List<LevelPassDef> Passes => levelPassConfig.Passes; // Native0x14bb9b0.

		public bool HasUnclaimedRewards => false;

		public LevelPassModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public override void Initialize()
		{
            // Native0x14bb594 and Lifetime callback0x14bddb8.
            base.Initialize();
            EnsurePassDataExists();
            EnemySpawner.OnDied += OnEnemyDied;
            Lifetime.Register(() => EnemySpawner.OnDied -= OnEnemyDied);
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
            // Native0x14bc21c.
            LevelPassDataUd pass = GetPassData(def.Key);
            return pass != null && pass.Purchased.Value;
		}

		public void MarkPassPurchased(string storeId)
		{
            // Native0x14bc398, predicate0x14bdecc. Match original store-specific ID through IAPManager.
            var iap = GameContext.Get<Codebase.In_apps.IAPManager>();
            foreach (LevelPassDef pass in levelPassConfig.Passes)
            {
                if (pass.ProductKey == Codebase.In_apps.ProductKey.None || iap.GetStoreId(pass.ProductKey) != storeId) continue;
                GetOrCreatePassData(pass.Key).Purchased.Value = true;
                return;
            }
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
            // Native0x14bb73c: append missing UD tiers, preserve existing progress and claim flags.
            foreach (LevelPassDef pass in levelPassConfig.Passes)
            {
                LevelPassDataUd data = GetOrCreatePassData(pass.Key);
                while (data.Tiers.Count < pass.Tiers.Count) data.Tiers.Create();
            }
		}

		private LevelPassDataUd GetPassData(string passKey)
		{
            // Native0x14bc2a0 and predicate0x14bdf0c: first matching persisted key, no creation.
            foreach (LevelPassDataUd pass in Data.Passes)
                if (pass.Key.Value == passKey) return pass;
            return null;
		}

		private LevelPassDataUd GetOrCreatePassData(string passKey)
		{
            // Native0x14bc530.
            LevelPassDataUd pass = GetPassData(passKey);
            if (pass == null)
            {
                pass = Data.Passes.Create();
                pass.Key.Value = passKey;
            }
            return pass;
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
