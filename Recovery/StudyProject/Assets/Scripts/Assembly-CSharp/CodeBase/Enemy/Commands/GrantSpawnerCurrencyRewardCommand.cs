using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerCurrencyRewardCommand : ICommand
	{
		public EnemySpawner Spawner;
	}
}
