using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public class EquipCrystalCommandHandler : ICommandHandler<EquipCrystalCommand>
	{
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private IAnalyticsWrapper analytics;
		public void Execute(EquipCrystalCommand command)
		{
			if (string.IsNullOrEmpty(command.SlotKey))
				return;
			var slot = crystalModel.GetSlot(command.SlotKey);
			if (slot == null || !slot.IsUnlocked)
				return;
			var previous = crystalModel.GetEquippedCrystal(command.SlotKey);
			if (previous != null && previous != command.Crystal)
			{
				crystalModel.RemoveCrystalModifiers(previous);
				previous.Unequip();
			}

			if (command.Crystal.IsEquipped)
				crystalModel.RemoveCrystalModifiers(command.Crystal);
			command.Crystal.Equip(command.SlotKey);
			crystalModel.ApplyCrystalModifiers(command.Crystal);
			analytics.Send(new Codebase.Analytics.Events.CrystalEquipEvent(command.Crystal, command.SlotKey));
			audioPlayer.PlaySoundOnce("crystal_equip");
		}
	}
}
