using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Analytics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Quests.Commands
{
	public class ClaimQuestChainRewardCommandHandler : ICommandHandler<ClaimQuestChainRewardCommand>
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CClaimAsync_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public ClaimQuestChainRewardCommand command;
			public ClaimQuestChainRewardCommandHandler _003C_003E4__this;
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
		private QuestConfig config;
		[Inject]
		private UINavigator uiNavigator;
		public void Execute(ClaimQuestChainRewardCommand command)
		{ // Original ELF 0x143a550; candidate only.
			ClaimAsync(command).Forget();
		}

		private async UniTaskVoid ClaimAsync(ClaimQuestChainRewardCommand command)
		{ // Original ELF 0x143a574; candidate only.
			var chain = config.GetChain(command.ChainKey);
			if (chain == null || chain.ChainReward == null || chain.ChainReward.Count == 0)
			{
				command.OnComplete?.Invoke();
				return;
			}

			var rewards = new List<Codebase.Rewards.RewardBase>(chain.ChainReward);
			var view = uiNavigator.Open<Codebase.UI.BossTower.BossRaidRewardView>("Assets/Prefabs/UI/BossTower/BossRaidRewardView.prefab");
			var completion = new UniTaskCompletionSource();
			view.Setup(rewards, () =>
			{
				foreach (var reward in rewards)
				{
					if (reward is Codebase.Rewards.StatReward stat)
						stat.Source = (Codebase.Unit.ModifierSource)18;
					App.ExecuteCommand(new Codebase.Rewards.Commands.GrantRewardCommand { Reward = reward, SourcePath = "quest_chain", SourceOverride = (Codebase.Unit.ModifierSource)18 });
				}

				command.OnComplete?.Invoke();
				completion.TrySetResult();
			});
			await completion.Task;
		}
	}
}
