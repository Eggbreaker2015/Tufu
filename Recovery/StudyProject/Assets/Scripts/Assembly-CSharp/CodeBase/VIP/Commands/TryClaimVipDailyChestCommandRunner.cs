using System;
using System.Linq;
using Codebase.VIP.Features;
using Codebase.Rewards.Commands;
using Codebase.Rewards.Flows;
using Codebase.UI.BossTower;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Core.UserData;
using Codebase.Rewards;
using Codebase.Services.Time;

namespace Codebase.VIP.Commands
{
	public struct TryClaimVipDailyChestCommandRunner : ICommandHandler<TryClaimVipDailyChestCommand>
	{
		[Inject]
		private VipModel vipModel;

		[Inject]
		private VipConfig vipConfig;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		public void Execute(TryClaimVipDailyChestCommand command)
		{
            // Native 0x15c2448. Bake before preview; grant only from the original collect callback.
            if (vipConfig.Features == null) return;
            DailyChestConfig config = vipConfig.Features.OfType<DailyChestConfig>().FirstOrDefault();
            if (config == null) return;
            int level = vipModel.Level;
            if (level < config.UnlockLevel || vipModel.DailyChestCooldownRemaining > TimeSpan.Zero) return;
            DailyChestLevelConfig levelConfig = config.GetActiveLevelConfig(level);
            if (levelConfig == null || levelConfig.Rewards == null || levelConfig.Rewards.Count == 0) return;
            App.ExecuteCommand(new BakeRewardsCommand { Rewards = levelConfig.Rewards, ForceBake = true, Multiplier = level });
            List<RewardBase> rewards = levelConfig.Rewards.Where(CanGrantReward).ToList();
            if (global::Codebase.Rpg.RpgModeConfig.Enabled)
            {
                rewards = global::Codebase.Rpg.RpgRewardFilter.Filter(rewards);
                if (rewards.Count == 0) { GrantRewards(rewards, vipModel, timeService, audioPlayer); return; }
            }
            if (rewards.Count == 0) return;
            VipModel model = vipModel;
            ITimeService clock = timeService;
            IAudioPlayer audio = audioPlayer;
            uiNavigator.Open<BossRaidRewardView>("Assets/Prefabs/UI/BossTower/BossRaidRewardView.prefab")
                .Setup(rewards, () => GrantRewards(rewards, model, clock, audio));
		}

		private static void GrantRewards(List<RewardBase> rewards, VipModel vipModel, ITimeService timeService, IAudioPlayer audioPlayer)
		{
            // Native 0x15c27d0: duplicate confirmations blocked by the stored UTC-midnight cooldown.
            if (vipModel.DailyChestCooldownRemaining > TimeSpan.Zero) return;
            audioPlayer.PlaySoundOnce("vip_chest_open");
            DateTime nextUtcDay = timeService.Now.ToDateTime().ToUniversalTime().Date.AddDays(1);
            vipModel.SetDailyChestCooldownTimestamp(unchecked((int)new DateTimeOffset(nextUtcDay).ToUnixTimeSeconds()));
            // Authored VIP level1/7/9/10 configurations contain only StatReward/ItemReward.
            // They grant synchronously before this shared flow's one-second presentation delay.
            UniTask delivery = App.InitiateFlow(new GrantRewardsFlow { Rewards = rewards, SourcePath = "vip.chest" });
            // Local learning adaptation: commit the real rewards and UTC-midnight cooldown together before Stop.
            GameContext.Get<GameDataService>().ForceSave();
            delivery.Forget();
		}

		private static bool CanGrantReward(RewardBase reward)
		{
            // Native 0x15c2adc: only zero/nonpositive stat rewards are filtered.
            if (!(reward is StatReward stat)) return true;
            if (stat.IsRandom || stat.UseGpm) return stat.BakedData != null && stat.BakedData.Value > 0f;
            return stat.Value > 0f;
		}
	}
}
