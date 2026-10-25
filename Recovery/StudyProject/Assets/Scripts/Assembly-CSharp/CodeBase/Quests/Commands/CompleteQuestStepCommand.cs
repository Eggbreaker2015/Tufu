using Codebase.Core.Commands;

namespace Codebase.Quests.Commands
{
	public struct CompleteQuestStepCommand : ICommand
	{
		public string StepKey;
	}
}
