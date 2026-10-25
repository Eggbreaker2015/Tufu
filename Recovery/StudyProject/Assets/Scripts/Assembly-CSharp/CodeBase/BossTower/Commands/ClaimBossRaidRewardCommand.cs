using Codebase.Core.Commands;

namespace Codebase.BossTower.Commands
{
	public struct ClaimBossRaidRewardCommand : ICommand
	{
		public string BossKey;
	}
}
