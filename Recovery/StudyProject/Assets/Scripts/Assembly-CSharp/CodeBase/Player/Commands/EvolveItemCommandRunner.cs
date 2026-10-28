using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Player.Models;
using Codebase.Rage;
using Codebase.Analytics.Events;
using Codebase.Abilities.Effects;
using Codebase.Player.Inventory;
using Codebase.Player.Flows;
using UniMob;

namespace Codebase.Player.Commands
{
	public struct EvolveItemCommandRunner : ICommandHandler<EvolveItemCommand>
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private RageModel rageModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		public void Execute(EvolveItemCommand command)
		{
			// Native 0x146851c: preserve the pre-evolution model before consuming copies.
			ItemModel item = command.ItemModel;
			if (item.IsEvolutionCapped)
				return;
			analytics.Send(new ItemEvolveEvent(item.Config.Key));
			playerModel.SelectedItem = null;
			var previous = new ItemModel(Lifetime.Eternal, item.Config) { Copies = item.Copies };
			if (item.Evolved)
			{
				previous.Evolved = true;
				previous.Level = item.Level;
			}
			bool wasEvolved = item.Evolved;
			item.Copies -= item.LevelsToEvolve;
			if (wasEvolved) item.Level++;
			else item.Evolved = true;
			GrantRageAccessEffect.RecalculateMaxLevel(playerModel.EquippedItems, rageModel);
			// The native call eagerly starts the flow and does not await it here.
			App.InitiateFlow(new EvolveItemFlow { OldItem = previous, NewItem = item, IsFirstEvolve = !wasEvolved }, false);
		}
	}
}
