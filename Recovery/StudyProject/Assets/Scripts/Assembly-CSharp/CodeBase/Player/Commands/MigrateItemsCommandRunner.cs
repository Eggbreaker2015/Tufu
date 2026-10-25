using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UserData;

namespace Codebase.Player.Commands
{
	public struct MigrateItemsCommandRunner : ICommandHandler<MigrateItemsCommand>
	{
		[Inject]
		private GameDataService gameDataService;

		public void Execute(MigrateItemsCommand command)
		{
		}
	}
}
