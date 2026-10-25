using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using System.Collections.Generic;
using UnityEngine;
using Codebase.Analytics.Events;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using Codebase.UI.BossTower;

namespace Codebase.BossTower.Commands
{
	public class ClaimBossRaidRewardCommandHandler : ICommandHandler<ClaimBossRaidRewardCommand>
	{
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private UINavigator uiNavigator;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private IAnalyticsWrapper analytics;
		public void Execute(ClaimBossRaidRewardCommand command)
		{
			if (!bossTowerModel.IsBossUnlocked(command.BossKey))
				return;
			if (!bossTowerModel.CanGrantBossClaimReward(command.BossKey))
			{
				Debug.LogError("Cannot grant raid reward for boss '" + command.BossKey + "': conditions not met");
				return;
			}

			if (!bossTowerModel.TrySpendTickets(1))
			{
				Debug.LogError("Cannot claim raid reward for boss '" + command.BossKey + "': no tickets available");
				return;
			}

			bossTowerModel.PortalVersion++;
			if (!bossTowerModel.TryGetBoss(command.BossKey, out BossModel boss))
			{
				Debug.LogError("Boss with key '" + command.BossKey + "' not found");
				return;
			}

			analytics.Send(new BossTowerRaidEvent(command.BossKey));
			List<RewardBase> rewards = BossRewardResolver.PreResolveAndBakeDisplay(new List<RewardBase>(boss.PreviousFloorRewardsBaked), itemsProvider);
			rewards.RemoveAll(reward => !(reward is ItemReward));
			uiNavigator.Open<BossRaidRewardView>("Assets/Prefabs/UI/BossTower/BossRaidRewardView.prefab").Setup(rewards, () =>
			{
				foreach (RewardBase reward in rewards)
					App.ExecuteCommand(new GrantRewardCommand { Reward = reward });
				bossTowerModel.IncrementDailyClaims(command.BossKey);
			});
			audioPlayer.PlaySoundOnce("boss_tower_raid");
		}
	}
}
