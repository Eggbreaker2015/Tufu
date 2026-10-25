using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public struct GenerateCrystalCommand : ICommand<CrystalItemModel>, ICommand
	{
		public CrystalRarity? ForcedRarity;

		public bool SkipGeodeSpend;

		public int BlacksmithingBonus;

		public int EnchantLevel;
	}
}
