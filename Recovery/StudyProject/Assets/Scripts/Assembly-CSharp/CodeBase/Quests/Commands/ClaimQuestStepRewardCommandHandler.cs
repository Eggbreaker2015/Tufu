using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Analytics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Meta;
using Codebase.Player.Systems;
using Codebase.Rewards;
using Codebase.Unit;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Quests.Commands
{
	public class ClaimQuestStepRewardCommandHandler : ICommandHandler<ClaimQuestStepRewardCommand>
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CClaimAsync_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public ClaimQuestStepRewardCommandHandler _003C_003E4__this;
			public ClaimQuestStepRewardCommand command;
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
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		[Inject]
		private UINavigator uiNavigator;
		[Inject]
		private PlayerRewardSystem playerRewardSystem;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		public void Execute(ClaimQuestStepRewardCommand command)
		{ // Original ELF 0x143ae90; candidate only.
			ClaimAsync(command).Forget();
		}

		private async UniTaskVoid ClaimAsync(ClaimQuestStepRewardCommand command)
		{ // Original ELF 0x143aeb4; candidate only.
			if (questModel.Prologue.IsPrologueStep(command.StepKey) || questModel.IsStepRewardClaimed(command.StepKey) || !questModel.IsStepComplete(command.StepKey))
				return;
			var step = config.GetStep(command.StepKey);
			if (step == null)
				return;
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && step.StatRewards != null && step.StatRewards.Count > 0)
			{
				var player = GameContext.Get<global::Codebase.Player.Models.PlayerModel>().Unit;
				global::Codebase.Rpg.RpgRewardService.Quest(step.LocationNumber, global::Codebase.Rpg.RpgModeConfig.Instance.QuestStepXpMultiplier, player != null ? player.transform.position : UnityEngine.Vector3.zero);
				questModel.MarkStepRewardClaimed(command.StepKey);
				analytics.Send(new Codebase.Analytics.Events.QuestsClaimEvent(command.StepKey));
				TryStartChainRewardTimer(command.StepKey);
				questModel.TryAdvanceToNextStep(command.StepKey);
				command.OnComplete?.Invoke();
				return;
			}
			var modifiers = BuildStatModifiers(step, command.StepKey);
			if (modifiers.Count != 0)
			{
				var rewards = BuildDisplayRewards(modifiers);
				var view = uiNavigator.Open<Codebase.UI.BossTower.BossRaidRewardView>("Assets/Prefabs/UI/BossTower/BossRaidRewardView.prefab");
				var completion = new UniTaskCompletionSource();
				view.Setup(rewards, () =>
				{
					foreach (var modifier in modifiers)
						playerRewardSystem.GrantStatReward(modifier, -1.0, 1);
					questModel.MarkStepRewardClaimed(command.StepKey);
					analytics.Send(new Codebase.Analytics.Events.QuestsClaimEvent(command.StepKey));
					TryStartChainRewardTimer(command.StepKey);
					questModel.TryAdvanceToNextStep(command.StepKey);
					command.OnComplete?.Invoke();
					completion.TrySetResult();
				});
				await completion.Task;
				return;
			}

			questModel.MarkStepRewardClaimed(command.StepKey);
			analytics.Send(new Codebase.Analytics.Events.QuestsClaimEvent(command.StepKey));
			TryStartChainRewardTimer(command.StepKey);
			questModel.TryAdvanceToNextStep(command.StepKey);
			command.OnComplete?.Invoke();
		}

		private void TryStartChainRewardTimer(string stepKey)
		{ // Original ELF 0x143af68; candidate only.
			var chain = config.GetChainOfStep(stepKey);
			if (chain == null || chain.IsPrologue || config.GetNextStep(stepKey) != null)
				return;
			questModel.StartChainRewardClaimTimer(chain.ChainKey);
		}

		private List<StatModifier> BuildStatModifiers(QuestStepDef stepDef, string stepKey)
		{ // Original ELF 0x143afdc; candidate only.
			var result = new List<StatModifier>();
			if (stepDef.StatRewards == null || stepDef.StatRewards.Count == 0)
				return result;
			double metaValue = metaStatsModel.Get("stat_drop").GetValue(stepDef.LocationNumber);
			foreach (var stat in stepDef.StatRewards)
				result.Add(new StatModifier { Value = stat.Value * (float)metaValue, StatType = stat.StatType, ModifierType = stat.ModifierType, Duration = stat.Duration, Name = "quest." + stepKey, Source = (ModifierSource)18 });
			return result;
		}

		private List<RewardBase> BuildDisplayRewards(List<StatModifier> modifiers)
		{ // Original ELF 0x143b324; candidate only.
			var result = new List<RewardBase>();
			foreach (var modifier in modifiers)
				result.Add(new StatReward { CustomModifier = true, Type = modifier.StatType, Value = modifier.Value, Source = modifier.Source, ModifierType = modifier.ModifierType, BakedData = new BakedStatReward { Type = modifier.StatType, Value = modifier.Value } });
			return result;
		}
	}
}
