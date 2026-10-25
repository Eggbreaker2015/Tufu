using Codebase.Core;
using System.Runtime.InteropServices;
using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	[StructLayout((LayoutKind)0, Size = 1)]
	public struct GrantSpawnerAllRewardsCommandHandler : ICommandHandler<GrantSpawnerAllRewardsCommand>
	{
		public void Execute(GrantSpawnerAllRewardsCommand command)
		{
            App.ExecuteCommand(new GrantSpawnerStatRewardCommand { Spawner = command.Spawner });
            App.ExecuteCommand<GrantSpawnerItemSlotRewardCommand, string>(new GrantSpawnerItemSlotRewardCommand { Spawner = command.Spawner });
            App.ExecuteCommand(new GrantSpawnerItemRewardCommand { Spawner = command.Spawner });
            App.ExecuteCommand(new GrantSpawnerGateRewardCommand { Spawner = command.Spawner });
            App.ExecuteCommand(new GrantSpawnerSoulRewardCommand { Spawner = command.Spawner });
        }
	}
}
