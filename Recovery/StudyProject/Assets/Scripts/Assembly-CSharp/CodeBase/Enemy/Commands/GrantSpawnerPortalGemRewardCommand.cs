using Codebase.Core.Commands;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerPortalGemRewardCommand : ICommand
	{
		public int GemIndex;

		public bool HasItemReward;
	}
}
