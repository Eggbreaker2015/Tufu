using Codebase.Core.Commands;

namespace Codebase.Events.Commands
{
	public struct MailUnclaimedCrystalRewardsCommand : ICommand
	{
		public string EventKey;
	}
}
