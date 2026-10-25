using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.Services.Time;

namespace Codebase.VIP.Commands
{
	public struct TryClaimVipDailyChestCommandRunner : ICommandHandler<TryClaimVipDailyChestCommand>
	{
		[Inject]
		private VipModel vipModel;

		[Inject]
		private VipConfig vipConfig;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		public void Execute(TryClaimVipDailyChestCommand command)
		{
		}

		private static void GrantRewards(List<RewardBase> rewards, VipModel vipModel, ITimeService timeService, IAudioPlayer audioPlayer)
		{
		}

		private static bool CanGrantReward(RewardBase reward)
		{
			return false;
		}
	}
}
