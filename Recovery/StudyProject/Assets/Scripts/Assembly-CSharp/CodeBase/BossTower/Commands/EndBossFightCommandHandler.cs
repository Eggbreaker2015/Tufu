using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using System.Collections.Generic;
using Codebase.Rewards;
using Codebase.Analytics.Events;
using Codebase.UI.BossTower;

namespace Codebase.BossTower.Commands
{
	public class EndBossFightCommandHandler : IFlowRunner<EndBossFightFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public EndBossFightCommandHandler _003C_003E4__this;
			public EndBossFightFlow flow;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private BossRaidSystem bossRaidSystem;
		[Inject]
		private UINavigator uiNavigator;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private IAnalyticsWrapper analytics;
		public async UniTask Execute(EndBossFightFlow flow)
		{
			if (!bossTowerModel.TryGetBoss(flow.BossKey, out BossModel boss))
				return;
			if (flow.Victory)
			{
				var rewards = BossRewardResolver.PreResolveAndBakeDisplay(new List<RewardBase>(boss.RewardsBaked), itemsProvider);
				// Pet mode: boss pet copies and universal shards ride along with the victory rewards (granted on hub/continue).
				PetEconomy.AppendBossWinRewards(rewards, boss.Key, itemsProvider);
				uiNavigator.Open<BossRaidVictoryView>("Assets/Prefabs/UI/BossTower/BossRaidVictoryView.prefab").Setup(boss, rewards, boss.Floor);
			}
			else
			{
				var health = bossRaidSystem.GetCurrentBossHealth();
				uiNavigator.Open<BossRaidDefeatView>("Assets/Prefabs/UI/BossTower/BossRaidDefeatView.prefab").Setup(boss, health.Item1, health.Item2, boss.Floor);
			}

			analytics.Send(new BossTowerFightEvent(flow.Victory ? "win" : "lose", flow.BossKey));
			audioPlayer.FadeMusic(true).Forget();
			if (flow.Victory)
			{
				bossTowerModel.MarkBossDefeated(boss.Key);
				bossTowerModel.IncrementBossFloor(boss.Key);
			}

			bossTowerModel.IsFighting = false;
			await UniTask.WaitForSeconds(1f);
			bossRaidSystem.EndRaid(flow.Victory);
		}
	}
}
