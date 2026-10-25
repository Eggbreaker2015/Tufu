using Codebase.Core.Commands;

namespace Codebase.BossTower.Commands
{
	public struct EnterBossArenaCommand : ICommand
	{
		public string BossKey;

		public bool IsCalledFromArena;

		public bool IsRetry;
	}
}
