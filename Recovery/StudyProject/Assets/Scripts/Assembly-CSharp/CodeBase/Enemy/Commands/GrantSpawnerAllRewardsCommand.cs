using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerAllRewardsCommand : ICommand
	{
		public EnemySpawner Spawner;
	}
}
