using Codebase.Core.Commands;
using Codebase.Player.Inventory;

namespace Codebase.Pet.Commands
{
	public struct UseUniversalShardCommand : ICommand
	{
		public ItemModel Item;

		public int Amount;
	}
}
