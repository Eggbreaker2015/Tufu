using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public struct UnequipCrystalCommand : ICommand
	{
		public CrystalItemModel Crystal;
	}
}
