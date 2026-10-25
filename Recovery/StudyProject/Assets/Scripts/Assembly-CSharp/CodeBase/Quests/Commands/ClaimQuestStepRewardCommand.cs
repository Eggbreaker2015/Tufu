using System;
using Codebase.Core.Commands;

namespace Codebase.Quests.Commands
{
	public struct ClaimQuestStepRewardCommand : ICommand
	{
		public string StepKey;

		public Action OnComplete;
	}
}
