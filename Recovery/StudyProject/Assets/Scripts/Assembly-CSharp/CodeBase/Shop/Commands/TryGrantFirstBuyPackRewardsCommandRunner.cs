using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;

namespace Codebase.Shop.Commands
{
	public struct TryGrantFirstBuyPackRewardsCommandRunner : ICommandHandler<TryGrantFirstBuyPackRewardsCommand>
	{
		[Inject]
		private UserModel userModel;

		[Inject]
		private FirstBuyPackRewardsConfig rewardsConfig;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		public void Execute(TryGrantFirstBuyPackRewardsCommand command)
		{
		}
	}
}
