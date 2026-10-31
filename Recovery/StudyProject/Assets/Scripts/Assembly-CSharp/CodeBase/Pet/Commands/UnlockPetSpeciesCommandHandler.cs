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
			return;
		}
	}
}
