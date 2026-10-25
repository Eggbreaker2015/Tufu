using Codebase.Core.Commands;

namespace Codebase.Quests.Commands
{
	public struct FocusNextQuestGiverCommand : ICommand
	{
		public string CompletedChainKey;
	}
}
