using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Economy;
using Codebase.Player.Models;

namespace Codebase.Pet.Commands
{
	public class UnlockPetSpeciesCommandHandler : ICommandHandler<UnlockPetSpeciesCommand>
	{
		[Inject]
		private CurrencyModel currencyModel;

		[Inject]
		private PlayerModel playerModel;

		public void Execute(UnlockPetSpeciesCommand command)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;
			if (!PetConfig.Enabled || !PetEconomy.CanUnlock(command.Config))
				return;
			if (!currencyModel.TrySpend(CurrencyKeys.PetUniversalShard, PetEconomy.GetUnlockCost(command.Config)))
				return;
			playerModel.CreateItem(command.Config, 1);
		}
	}
}
