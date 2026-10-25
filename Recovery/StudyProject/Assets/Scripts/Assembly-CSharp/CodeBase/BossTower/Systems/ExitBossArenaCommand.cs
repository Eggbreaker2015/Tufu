using Codebase.Core.Commands;

namespace Codebase.BossTower.Systems
{
	public struct ExitBossArenaCommand : ICommand
	{
		public string bossKey;
	}
}
