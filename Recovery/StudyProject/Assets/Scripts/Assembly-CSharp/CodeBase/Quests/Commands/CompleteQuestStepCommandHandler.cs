using Codebase.Core;
using Codebase.Core.Commands;
using System;
using System.Collections.Generic;
using Codebase.Analytics;

namespace Codebase.Quests.Commands
{
	public class CompleteQuestStepCommandHandler : ICommandHandler<CompleteQuestStepCommand>
	{
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		public void Execute(CompleteQuestStepCommand command)
		{ // Original ELF 0x143bf98; candidate only.
			if (questModel.IsStepComplete(command.StepKey))
				return;
			questModel.MarkComplete(command.StepKey);
			var chain = config.GetChainOfStep(command.StepKey);
			if (chain == null || !chain.IsPrologue)
				return;
			questModel.MarkStepRewardClaimed(command.StepKey);
			questModel.MarkChainRewardClaimed(chain.ChainKey);
		}
	}
}
