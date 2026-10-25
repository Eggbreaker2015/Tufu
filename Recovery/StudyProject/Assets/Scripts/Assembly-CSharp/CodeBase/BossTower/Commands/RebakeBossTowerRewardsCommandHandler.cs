using Codebase.Core;
using Codebase.Core.Commands;
using System.Linq;

namespace Codebase.BossTower.Commands
{
	public class RebakeBossTowerRewardsCommandHandler : ICommandHandler<RebakeBossTowerRewardsCommand>
	{
		[Inject]
		private BossTowerModel bossTowerModel;
		public void Execute(RebakeBossTowerRewardsCommand command)
		{
			foreach (BossModel boss in bossTowerModel.Bosses)
				bossTowerModel.RebakeRewards(boss);
			if (bossTowerModel.Bosses.All(boss => !boss.IsWeakened))
				bossTowerModel.SelectRandomWeakenedBoss();
		}
	}
}
