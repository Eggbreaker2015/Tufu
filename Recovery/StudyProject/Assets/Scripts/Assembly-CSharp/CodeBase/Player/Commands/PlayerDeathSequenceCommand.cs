using Codebase.Core.Commands;

namespace Codebase.Player.Commands
{
	public struct PlayerDeathSequenceCommand : ICommand
	{
		public string Killer;
	}
}
