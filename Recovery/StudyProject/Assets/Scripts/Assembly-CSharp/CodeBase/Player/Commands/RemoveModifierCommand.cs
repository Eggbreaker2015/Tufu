using Codebase.Core.Commands;
using Codebase.Unit;

namespace Codebase.Player.Commands
{
	public struct RemoveModifierCommand : ICommand
	{
		public UnitCore Unit;

		public string Name;

		public StatType StatType;
	}
}
