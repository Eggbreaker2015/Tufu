using System;
using System.Collections.Generic;
using Multicast.UserData;

namespace Codebase.Core.UserData
{
    public static class UserDataProvider
    {
        private static readonly Dictionary<Type, Func<GameUserData, UdObject>> Resolvers =
            new Dictionary<Type, Func<GameUserData, UdObject>>
            {
                { typeof(global::Codebase.Player.Models.PlayerUd), data => data.Player },
                { typeof(global::Codebase.Analytics.UserDataUd), data => data.User },
                { typeof(global::Codebase.VIP.VipUd), data => data.Vip },
                { typeof(global::Codebase.Events.Models.EventsUd), data => data.Events },
                { typeof(global::Codebase.Mail.MailRepoUd), data => data.Mails },
                { typeof(global::Codebase.Shop.ShopUd), data => data.Shop },
                { typeof(global::Codebase.Gates.GatesUd), data => data.Gates },
                { typeof(global::Codebase.Cashback.CashbackUd), data => data.Cashback },
                { typeof(global::Codebase.Analytics.AnalyticsUd), data => data.Analytics },
                { typeof(global::Codebase.Events.Analytics.EventAnalyticsUd), data => data.EventAnalytics },
                { typeof(global::Codebase.ABTests.ABTestUd), data => data.ABTests },
                { typeof(global::Codebase.Rewards.BakedRewardsUd), data => data.BakedRewards },
                { typeof(global::Codebase.BossTower.BossTowerUd), data => data.BossTower },
                { typeof(global::Codebase.Amulet.AmuletUd), data => data.Amulet },
                { typeof(global::Codebase.Crystals.CrystalUd), data => data.Crystals },
                { typeof(global::Codebase.LevelPass.LevelPassUd), data => data.LevelPass },
                { typeof(global::Codebase.Location.IslandMasteryUd), data => data.IslandMastery },
                { typeof(global::Codebase.Economy.CurrencyUd), data => data.Currency },
                { typeof(global::Codebase.Player.DailyProcs.DailyProcsUd), data => data.DailyProcs },
                { typeof(global::Codebase.Meta.MetaStatsUd), data => data.MetaStats },
                { typeof(global::Codebase.SecretChest.SecretChestsUd), data => data.SecretChests },
                { typeof(global::Codebase.Quests.QuestsUd), data => data.Quests },
                { typeof(global::Codebase.Mines.MinesUd), data => data.Mines },
                { typeof(global::Codebase.Notifications.NotificationsUd), data => data.Notifications },
                { typeof(global::Codebase.PromoCodes.PromoCodesUd), data => data.PromoCodes },
            };

        public static TData Resolve<TData>() where TData : UdObject
        {
            var service = GameContext.Get<GameDataService>();
            if (Resolvers.TryGetValue(typeof(TData), out var resolver))
                return (TData)resolver(service.Data);
            throw new InvalidOperationException("No resolver registered for type " + typeof(TData).Name);
        }
    }
}
