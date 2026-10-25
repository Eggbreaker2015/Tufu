using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Mail;
using Codebase.Services.Time;

namespace Codebase.SoulPass.Commands
{
	public class SendSoulPassDailyMailCommandHandler : ICommandHandler<SendSoulPassDailyMailCommand>
	{
		[Inject]
		private UserModel userModel;

		[Inject]
		private AmuletConfig amuletConfig;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private MailsRepository mailsRepository;

		private const string MailKeyPrefix = "soul_pass_daily_";

		private const string TemplateKey = "soul_pass_daily";

		private const int ArchiveAfterDays = 14;

		public void Execute(SendSoulPassDailyMailCommand command)
		{
		}
	}
}
