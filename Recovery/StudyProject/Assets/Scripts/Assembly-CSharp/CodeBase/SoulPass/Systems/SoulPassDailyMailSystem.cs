using System;
using Codebase.Core;
using Codebase.Services.Time;
using Codebase.SoulPass.Commands;
using Scellecs.Morpeh;

namespace Codebase.SoulPass.Systems
{
    public class SoulPassDailyMailSystem : SystemBase
    {
        [Inject] private ITimeService timeService;
        private DateTime lastCheckedDate;

        public override void OnAwake()
        {
            // Native0x13d9f0c. Existing command now performs local direct reward delivery.
            lastCheckedDate = timeService.Now.ToDateTime().ToUniversalTime().Date;
            App.ExecuteCommand(new SendSoulPassDailyMailCommand());
        }

        public override void OnUpdate(float deltaTime)
        {
            // Native0x13da05c checks only a forward UTC-date change; no missed-day catch-up loop.
            DateTime todayUtc = timeService.Now.ToDateTime().ToUniversalTime().Date;
            if (todayUtc <= lastCheckedDate) return;
            App.ExecuteCommand(new SendSoulPassDailyMailCommand());
            lastCheckedDate = todayUtc;
        }
    }
}
