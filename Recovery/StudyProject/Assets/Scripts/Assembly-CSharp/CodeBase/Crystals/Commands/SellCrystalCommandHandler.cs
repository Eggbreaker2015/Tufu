using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public class SellCrystalCommandHandler : ICommandHandler<SellCrystalCommand>
	{
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		public void Execute(SellCrystalCommand command)
		{
			if (command.Crystal.IsEquipped || command.Crystal.IsProtected)
				return;
			double value = crystalModel.CalculateSellPrice(command.Crystal);
			crystalModel.AddMagicDust(value);
			crystalModel.RemoveCrystal(command.Crystal);
			audioPlayer.PlaySoundOnce("crystal_sell");
		}
	}
}
