using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public struct SellCrystalCommand : ICommand
	{
		public CrystalItemModel Crystal;
	}
}
