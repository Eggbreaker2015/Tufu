using System;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UserData;
using Codebase.Economy;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using Codebase.Services.Time;

namespace Codebase.SoulPass.Commands
{
    // Learning adaptation: preserve the existing command entry but deliver original daily rewards directly.
    // Mailbox restoration is explicitly excluded; this handler does not create/read mail.
    public class SendSoulPassDailyMailCommandHandler : ICommandHandler<SendSoulPassDailyMailCommand>
    {
        [Inject] private UserModel userModel;
        [Inject] private AmuletConfig amuletConfig;
        [Inject] private ITimeService timeService;

        public void Execute(SendSoulPassDailyMailCommand command)
        {
            // Native0x13da1bc requires the purchased flag and uses the game's clock converted to UTC date.
            if (!userModel.SoulPassPurchased.Value) return;
            DateTime todayUtc = timeService.Now.ToDateTime().ToUniversalTime().Date;
            double todayTicks = todayUtc.Ticks;
            // Original field stores DateTime.Ticks as double, not Unix seconds.
            // Original dedup used dated mail keys; direct delivery instead reserves the same day in this persisted field.
            if (userModel.SoulPassLastDailyMailTime.Value >= todayTicks) return;
            userModel.SoulPassLastDailyMailTime.Value = todayTicks;
            App.ExecuteCommand(new GrantRewardCommand
            {
                Reward = new CurrencyReward { Key = CurrencyKeys.BossTowerTickets, Amount = amuletConfig.PassDailyBossTickets },
                SourcePath = "soul_pass_daily"
            });
            App.ExecuteCommand(new GrantRewardCommand
            {
                Reward = new SoulReward { SoulKey = "great", Amount = amuletConfig.PassDailyLegendarySouls },
                SourcePath = "soul_pass_daily"
            });
            // Local direct-delivery completion: save both daily reservation and real reward balances together.
            GameContext.Get<GameDataService>().ForceSave();
        }
    }
}
