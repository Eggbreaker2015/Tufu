using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerEventCurrencyRewardCommand : ICommand
	{
		public EnemySpawner Spawner;

		public EnemyUnit Unit;
	}
}
