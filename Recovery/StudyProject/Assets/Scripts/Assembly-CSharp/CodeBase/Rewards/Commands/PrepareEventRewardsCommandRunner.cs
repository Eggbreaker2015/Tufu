using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Events.Models;
using Codebase.Simulation.Systems;

namespace Codebase.Rewards.Commands
{
	public struct PrepareEventRewardsCommandRunner : ICommandHandler<PrepareEventRewardsCommand>
	{
		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private SimulationSystem simulationSystem;

		public void Execute(PrepareEventRewardsCommand command)
		{
		}

		private void PrepareSuccessRewards(EventModel eventModel)
		{
		}

		private void PrepareFailedRewards(EventModel eventModel)
		{
		}
	}
}
