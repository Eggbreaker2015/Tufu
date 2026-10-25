using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.BossTower;
using Codebase.Cashback;
using Codebase.Crystals;
using Codebase.Economy;
using Codebase.Enemy;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Gates;
using Codebase.LevelPass;
using Codebase.Location;
using Codebase.Mail;
using Codebase.Meta;
using Codebase.Mines;
using Codebase.Notifications;
using Codebase.Player.DailyProcs;
using Codebase.Player.Models;
using Codebase.PromoCodes;
using Codebase.Quests;
using Codebase.Rewards;
using Codebase.SecretChest;
using Codebase.Shop;
using Codebase.VIP;
using Multicast.UserData;

namespace Codebase.Core.UserData
{
	public sealed class GameUserData : UdObject
	{
		public PlayerUd Player { get; }

		public UserDataUd User { get; }

		public VipUd Vip { get; }

		public EventsUd Events { get; }

		public MailRepoUd Mails { get; }

		public ShopUd Shop { get; }

		public GatesUd Gates { get; }

		public CashbackUd Cashback { get; }

		public AnalyticsUd Analytics { get; }

		public EventAnalyticsUd EventAnalytics { get; }

		public ABTestUd ABTests { get; }

		public SpawnersUd Spawners { get; }

		public BakedRewardsUd BakedRewards { get; }

		public BossTowerUd BossTower { get; }

		public AmuletUd Amulet { get; }

		public CrystalUd Crystals { get; }

		public LevelPassUd LevelPass { get; }

		public IslandMasteryUd IslandMastery { get; }

		public CurrencyUd Currency { get; }

		public DailyProcsUd DailyProcs { get; }

		public MetaStatsUd MetaStats { get; }

		public SecretChestsUd SecretChests { get; }

		public QuestsUd Quests { get; }

		public MinesUd Mines { get; }

		public NotificationsUd Notifications { get; }

		public PromoCodesUd PromoCodes { get; }

		public GameUserData(UdArgs args)
			: base(args)
		{
			Player = new global::Codebase.Player.Models.PlayerUd(Child("player", false));
			User = new global::Codebase.Analytics.UserDataUd(Child("user", true));
			Vip = new global::Codebase.VIP.VipUd(Child("vip", true));
			Events = new global::Codebase.Events.Models.EventsUd(Child("events", true));
			Mails = new global::Codebase.Mail.MailRepoUd(Child("mails", true));
			Shop = new global::Codebase.Shop.ShopUd(Child("shop", true));
			Gates = new global::Codebase.Gates.GatesUd(Child("gates", true));
			Cashback = new global::Codebase.Cashback.CashbackUd(Child("cashback", true));
			Analytics = new global::Codebase.Analytics.AnalyticsUd(Child("analytics", true));
			EventAnalytics = new global::Codebase.Events.Analytics.EventAnalyticsUd(Child("event_analytics", true));
			ABTests = new global::Codebase.ABTests.ABTestUd(Child("ab_tests", true));
			Spawners = new global::Codebase.Enemy.SpawnersUd(Child("spawners", true));
			BakedRewards = new global::Codebase.Rewards.BakedRewardsUd(Child("baked_rewards", true));
			BossTower = new global::Codebase.BossTower.BossTowerUd(Child("boss_tower", true));
			Amulet = new global::Codebase.Amulet.AmuletUd(Child("amulet", true));
			Crystals = new global::Codebase.Crystals.CrystalUd(Child("crystals", true));
			LevelPass = new global::Codebase.LevelPass.LevelPassUd(Child("level_pass", true));
			IslandMastery = new global::Codebase.Location.IslandMasteryUd(Child("island_mastery", true));
			Currency = new global::Codebase.Economy.CurrencyUd(Child("currency", true));
			DailyProcs = new global::Codebase.Player.DailyProcs.DailyProcsUd(Child("daily_procs", true));
			MetaStats = new global::Codebase.Meta.MetaStatsUd(Child("meta_stats", true));
			SecretChests = new global::Codebase.SecretChest.SecretChestsUd(Child("secret_chests", true));
			Quests = new global::Codebase.Quests.QuestsUd(Child("quests", true));
			Mines = new global::Codebase.Mines.MinesUd(Child("mines", true));
			Notifications = new global::Codebase.Notifications.NotificationsUd(Child("notifications", true));
			PromoCodes = new global::Codebase.PromoCodes.PromoCodesUd(Child("promo_codes", true));
		}
	}
}
