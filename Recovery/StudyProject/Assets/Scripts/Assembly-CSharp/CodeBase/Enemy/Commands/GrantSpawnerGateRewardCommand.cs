using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerGateRewardCommand : ICommand
	{
		public EnemySpawner Spawner;
	}
}
