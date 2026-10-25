using System.Collections.Generic;
using Codebase.Core.Commands;

namespace Codebase.Rewards.Commands
{
	public struct BakeRewardsCommand : ICommand
	{
		public IEnumerable<RewardBase> Rewards;

		public bool ForceBake;

		public float Multiplier;

		public bool BakeRosterEvent;
	}
}
