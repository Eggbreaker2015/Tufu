using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Services.Time;
using Codebase.Simulation.Forecast;
using Codebase.Unit;

namespace Codebase.Events.Commands
{
	public class MailUnclaimedCrystalRewardsCommandHandler : ICommandHandler<MailUnclaimedCrystalRewardsCommand>
	{
		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private ITimeService timeService;

		public void Execute(MailUnclaimedCrystalRewardsCommand command)
		{
		}

		private List<ForecastCrystalRewardEntry> CollectUnclaimedMilestoneRewards(EventModel model, ForecastEventPlan plan, out int newClaimedCount)
		{
			newClaimedCount = default;
			return null;
		}

		private bool TryBuildStatModifier(StatReward stat, out StatModifier modifier)
		{
			modifier = null;
			return false;
		}

		private bool TryBuildMailSku(RewardBase reward, out string sku)
		{
			sku = null;
			return false;
		}
	}
}
