using Codebase.Core;
using Codebase.Core.Commands;
using System.Linq;

namespace Codebase.Crystals.Commands
{
	public class SellAllUnprotectedCrystalsCommandHandler : ICommandHandler<SellAllUnprotectedCrystalsCommand>
	{
		[Inject]
		private CrystalModel crystalModel;
		public void Execute(SellAllUnprotectedCrystalsCommand command)
		{
			var crystals = crystalModel.Crystals.Where(crystal => !crystal.IsEquipped && !crystal.IsProtected).ToList();
			if (crystals.Count == 0)
				return;
			foreach (var crystal in crystals)
			{
				double value = crystalModel.CalculateSellPrice(crystal);
				crystalModel.AddMagicDust(value);
				crystalModel.RemoveCrystal(crystal);
			}
		}
	}
}
