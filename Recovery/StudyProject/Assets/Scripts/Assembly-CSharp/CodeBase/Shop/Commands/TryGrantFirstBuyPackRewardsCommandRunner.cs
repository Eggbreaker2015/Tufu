using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Analytics.Events;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UserData;
using Codebase.Rewards;
using Codebase.Rewards.Flows;
using Cysharp.Threading.Tasks;

namespace Codebase.Shop.Commands
{
    public struct TryGrantFirstBuyPackRewardsCommandRunner : ICommandHandler<TryGrantFirstBuyPackRewardsCommand>
    {
        [Inject] private UserModel userModel;
        [Inject] private FirstBuyPackRewardsConfig rewardsConfig;
        [Inject] private IAnalyticsWrapper analyticsWrapper;
        private static readonly HashSet<UserModel> granting = new HashSet<UserModel>();

        public void Execute(TryGrantFirstBuyPackRewardsCommand command)
        {
            // Native 0x1403d50 marks the one-shot claim before initiating its reward flow.
            // The paid guard is a learning adaptation for the intentionally exposed entrance.
            if (userModel.InAppsBoughtValueInRewarded.Value <= 0f || userModel.FirstBuyPackRewardsGranted.Value || !granting.Add(userModel)) return;
            Grant(userModel, rewardsConfig, analyticsWrapper).Forget();
        }

        private static async UniTask Grant(UserModel user, FirstBuyPackRewardsConfig config, IAnalyticsWrapper analytics)
        {
            try
            {
                var rewards = new List<RewardBase>(config.Rewards);
                var save = GameContext.Get<GameDataService>();
                user.FirstBuyPackRewardsGranted.Value = true;
                // GrantRewardsFlow grants this configuration's three ItemRewards and one StatReward
                // synchronously before its first await. It contains no deferred ChestReward.
                UniTask delivery = App.InitiateFlow(new GrantRewardsFlow { Rewards = rewards, SourcePath = "shop" });
                // Local completion adaptation: persist the marker and actual rewards together,
                // before the flow's one-second presentation delay can be interrupted by Editor Stop.
                save.ForceSave();
                await delivery;
                analytics.Send(new FirstBuyPackRewardsGrantedEvent());
            }
            finally { granting.Remove(user); }
        }

        [StaticReset] private static void ResetStaticState() => granting.Clear();
    }
}
