using System;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Rewards.Commands;
using Codebase.Shop;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Codebase.Rewards.Flows
{
    public class GrantRewardsFlowRunner : IFlowRunner<GrantRewardsFlow>, IFlowHandlerBase
    {
        [Inject] private ChestsConfig chestsConfig;
        [Inject] private ChestQueueModel chestQueue;

        public async UniTask Execute(GrantRewardsFlow flow)
        {
            // Native wrapper0x1419e0c and bounded async MoveNext0x1419fd0-0x141a818.
            if (flow.Rewards == null || flow.Rewards.Count == 0) return;
            foreach (RewardBase reward in flow.Rewards)
            {
                if (reward is ChestReward) continue;
                App.ExecuteCommand(new GrantRewardCommand { Reward = reward, SourcePath = flow.SourcePath });
            }
            // Original Delay is1 second, scaled time, PlayerLoopTiming numeric8.
            await UniTask.Delay(TimeSpan.FromSeconds(1.0), false, (PlayerLoopTiming)8);
            foreach (RewardBase reward in flow.Rewards)
                if (reward is ChestReward chest) EnqueueChest(chest);
            // Queue processing belongs to the existing App update flow. The original does not await queue-empty here.
            App.RequestAppUpdateFlow();
        }

        private void EnqueueChest(ChestReward chestReward)
        {
            // Native0x1419ec4: nonpositive amounts are skipped; no invented fallback chest.
            if (chestReward == null || chestReward.Amount <= 0) return;
            ChestDef chest = chestsConfig.Get(chestReward.Key);
            if (chest == null)
            {
                Debug.LogError("Chest with key " + chestReward.Key + " not found");
                return;
            }
            chestQueue.EnqueueChest(chest, chestReward.Amount);
        }
    }
}
