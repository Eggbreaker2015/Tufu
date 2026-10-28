using Codebase.Core.Commands;
using Codebase.Player.Inventory;

namespace Codebase.Pet.Commands
{
	public struct UnlockPetSpeciesCommand : ICommand
	{
		public ItemConfig Config;
	}
}
