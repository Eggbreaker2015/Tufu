using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerItemRewardCommand : ICommand
	{
		public EnemySpawner Spawner;

		public EnemyUnit Unit;

		public int Copies;
	}
}
