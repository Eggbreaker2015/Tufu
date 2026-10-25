using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerStatRewardCommand : ICommand
	{
		public EnemySpawner Spawner;

		public EnemyUnit Unit;
	}
}
