using Codebase.Core;
using Codebase.Mail;
using Codebase.Services.Time;
using Codebase.VIP;
using Scellecs.Morpeh;

namespace Codebase.Cashback.Systems
{
	public class CashbackSystem : SystemBase
	{
		[Inject]
		private CashbackModel cashbackModel;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private CashBackConfig config;

		[Inject]
		private MailsRepository mailsRepository;

		[Inject]
		private VipModel vipModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void SendCashback(int nowPeriodId)
		{
		}
	}
}
