using Codebase.Core;
using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public class UnequipCrystalCommandHandler : ICommandHandler<UnequipCrystalCommand>
	{
		[Inject]
		private CrystalModel crystalModel;
		public void Execute(UnequipCrystalCommand command)
		{
			crystalModel.RemoveCrystalModifiers(command.Crystal);
			command.Crystal.Unequip();
		}
	}
}
