using System.Collections.Generic;
using Codebase.Core.Flows;

namespace Codebase.Rewards.Flows
{
	public struct GrantRewardsFlow : IFlow
	{
		public List<RewardBase> Rewards;

		public string SourcePath;
	}
}
