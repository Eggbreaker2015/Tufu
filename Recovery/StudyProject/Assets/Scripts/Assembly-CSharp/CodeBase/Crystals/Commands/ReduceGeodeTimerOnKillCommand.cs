using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public struct ReduceGeodeTimerOnKillCommand : ICommand
	{
		public int KillCount;
	}
}
