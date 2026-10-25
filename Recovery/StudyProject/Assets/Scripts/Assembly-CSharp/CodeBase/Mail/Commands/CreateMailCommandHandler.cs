using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;

namespace Codebase.Mail.Commands
{
	public class CreateMailCommandHandler : ICommandHandler<CreateMailCommand>
	{
		[Inject]
		private MailsRepository mailsRepository;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		public void Execute(CreateMailCommand command)
		{
		}
	}
}
