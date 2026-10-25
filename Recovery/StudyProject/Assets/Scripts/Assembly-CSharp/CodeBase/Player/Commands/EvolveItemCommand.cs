using Codebase.Core.Commands;
using Codebase.Player.Inventory;

namespace Codebase.Player.Commands
{
	public struct EvolveItemCommand : ICommand
	{
		public ItemModel ItemModel;
	}
}
