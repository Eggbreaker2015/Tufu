using System.Runtime.InteropServices;
using Codebase.Core.Commands;

namespace Codebase.SoulPass.Commands
{
	[StructLayout((LayoutKind)0, Size = 1)]
	public struct SendSoulPassDailyMailCommand : ICommand
	{
	}
}
