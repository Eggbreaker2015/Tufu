using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Simulation.Systems;
using Codebase.Unit;

namespace Codebase.Rewards.Commands
{
	public struct BakeRewardsCommandRunner : ICommandHandler<BakeRewardsCommand>
	{
		[Inject]
		private SimulationSystem simulationSystem;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private EventsRepository eventsRepository;

		private static readonly List<StatType> randomStats;

		public void Execute(BakeRewardsCommand command)
		{
		}

		private IEnumerable<RewardBase> GetRosterEventRewards()
		{
			return null;
		}

		private void BakeStatReward(StatReward statReward, float multiplier, bool forceBake)
		{
		}

		private BakedStatReward CreateBakedStatReward(StatReward reward, float multiplier)
		{
			return null;
		}

		private void BakeItemReward(ItemReward itemReward, bool forceBake)
		{
		}
	}
}
