using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerSoulRewardCommand : ICommand
	{
		public EnemySpawner Spawner;
	}
}
