using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public struct EquipCrystalCommand : ICommand
	{
		public CrystalItemModel Crystal;

		public string SlotKey;
	}
}
