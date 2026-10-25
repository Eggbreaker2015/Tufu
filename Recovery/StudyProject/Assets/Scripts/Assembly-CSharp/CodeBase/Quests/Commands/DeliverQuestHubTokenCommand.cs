using Codebase.Core.Commands;

namespace Codebase.Quests.Commands
{
	public struct DeliverQuestHubTokenCommand : ICommand
	{
		public string ChainKey;

		public string TokenKey;
	}
}
