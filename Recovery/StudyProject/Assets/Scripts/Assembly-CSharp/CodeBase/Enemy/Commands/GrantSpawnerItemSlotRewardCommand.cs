using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerItemSlotRewardCommand : ICommand<string>, ICommand
	{
		public EnemySpawner Spawner;
	}
}
