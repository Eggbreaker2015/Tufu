using Codebase.Core.Commands;
using Codebase.Unit;

namespace Codebase.Player.Commands
{
	public struct AddTemporaryModifierCommand : ICommand
	{
		public UnitCore Unit;

		public string Name;

		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		public float Value;

		public float Duration;

		public bool RemoveDuplicates;
	}
}
