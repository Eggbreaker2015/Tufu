using System.Runtime.InteropServices;
using Codebase.Core.Commands;

namespace Codebase.Player.Commands
{
	[StructLayout((LayoutKind)0, Size = 1)]
	public struct RemoveModifierCommandHandler : ICommandHandler<RemoveModifierCommand>
	{
		public void Execute(RemoveModifierCommand command)
		{
		}
	}
}
