using Codebase.Core.Commands;
using Codebase.Unit;

namespace Codebase.Rewards.Commands
{
	public struct GrantRewardCommand : ICommand
	{
		public RewardBase Reward;

		public string SourcePath;

		public ModifierSource? SourceOverride;
	}
}
