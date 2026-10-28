using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Economy;
using Codebase.Player.Inventory;
using Codebase.UI.World;

namespace Codebase.Pet.Commands
{
	public class UseUniversalShardCommandHandler : ICommandHandler<UseUniversalShardCommand>
	{
		[Inject]
		private CurrencyModel currencyModel;

		public void Execute(UseUniversalShardCommand command)
		{
			ItemModel item = command.Item;
			if (!PetConfig.Enabled || item == null || item.IsArmor || command.Amount <= 0 || PetEconomy.IsEvolutionCapped(item))
				return;
			if (!currencyModel.TrySpend(CurrencyKeys.PetUniversalShard, command.Amount))
				return;
			int previousCopies = item.Copies;
			item.Copies += command.Amount;
			GameContext.Get<WeaponRewardView>().Setup(item, previousCopies, false, true);
		}
	}
}
