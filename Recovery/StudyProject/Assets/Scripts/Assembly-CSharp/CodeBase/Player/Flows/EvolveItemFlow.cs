using Codebase.Core.Flows;
using Codebase.Player.Inventory;

namespace Codebase.Player.Flows
{
	public struct EvolveItemFlow : IFlow
	{
		public ItemModel OldItem;

		public ItemModel NewItem;

		public bool IsFirstEvolve;
	}
}
