using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public struct EnchantCrystalCommand : ICommand<EnchantAttempt>, ICommand
	{
		public CrystalItemModel Crystal;

		public bool Guaranteed;
	}
}
