using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Meta;

namespace Codebase.Crystals.Commands
{
	public struct ReduceGeodeTimerOnKillCommandHandler : ICommandHandler<ReduceGeodeTimerOnKillCommand>
	{
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		public void Execute(ReduceGeodeTimerOnKillCommand command)
		{
			if (command.KillCount < 1)
				return;
			double value = metaStatsModel.Get("crystal_timer_on_kill_reduce").Value;
			if (value <= 0.0)
				return;
			crystalModel.ReduceGeodeTimer(value * (double)command.KillCount);
		}
	}
}
