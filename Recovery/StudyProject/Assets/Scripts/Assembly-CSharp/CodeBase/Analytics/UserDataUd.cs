using Multicast.UserData;

namespace Codebase.Analytics
{
	public class UserDataUd : UdObject
	{
		public UdValue<string> UserId { get; }

		public UdValue<int> PlaytimeMinutes { get; }

		public UdValue<int> PlaytimeSeconds { get; }

		public UdValue<float> PlaytimeElapsed { get; }

		public UdValue<int> ActiveMinutes { get; }

		public UdValue<int> ActiveSeconds { get; }

		public UdValue<int> FakePlaytimeMinutes { get; }

		public UdValue<bool> SoundEnabled { get; }

		public UdValue<bool> MusicEnabled { get; }

		public UdValue<bool> HapticsEnabled { get; }

		public UdValue<bool> HighQualityEnabled { get; }

		public UdValue<float> SoundVolume { get; }

		public UdValue<float> MusicVolume { get; }

		public UdValue<int> RewardedWatchTimes { get; }

		public UdValue<int> RewardedTryAttempt { get; }

		public UdValue<float> LoadingDelay { get; }

		public UdValue<float> InAppsBoughtValueInRewarded { get; }

		public UdValue<bool> RewardedDelayFromStartUnlocked { get; }

		public UdValue<int> GpmIndex { get; }

		public UdValue<int> PoolIndex { get; }

		public UdValue<float> OptimumTime { get; }

		public UdValue<int> Impressions { get; }

		public UdValue<float> AdRevenue { get; }

		public UdValue<float> Ecpm { get; }

		public UdValue<double> InstallTime { get; }

		public UdValue<int> DaysSinceInstall { get; }

		public UdValue<string> AppVersion { get; }

		public UdValue<string> FbAchievements { get; }

		public UdValue<int> BannerImpressions { get; }

		public UdValue<float> BannerAdRevenue { get; }

		public UdValue<float> BannerEcpm { get; }

		public UdValue<bool> CompletedRateUs { get; }

		public UdListObject<RewardedWeightUd> RewardedWeights { get; }

		public UdValue<bool> FirstBuyPackRewardsGranted { get; }

		public UdValue<int> DailyAdsWatched { get; }

		public UdValue<double> DailyAdsResetTime { get; }

		public UdValue<bool> NoAdsPurchased { get; }

		public UdValue<bool> SoulPassPurchased { get; }

		public UdValue<bool> CrystalPassPurchased { get; }

		public UdValue<double> SoulPassPurchaseTime { get; }

		public UdValue<double> CrystalPassPurchaseTime { get; }

		public UdValue<double> SoulPassLastDailyMailTime { get; }

		public UdValue<bool> ChaoticSoulOfferPurchased { get; }

		public UdValue<double> ChaoticSoulOfferPurchaseTime { get; }

		public UdDictValue<int> FreeBoosterTokens { get; }

        // Learning-only simulated orders; saved atomically with the granted rewards in this profile.
        public UdDictValue<string> LocalPurchaseTransactions { get; }

		public UserDataUd(UdArgs args) : base(args)
{
    UserId = new global::Multicast.UserData.UdValue<global::System.String>(Child("user_id", true));
    PlaytimeMinutes = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("playtime_minutes", true));
    PlaytimeSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("playtime_seconds", true));
    PlaytimeElapsed = new global::Multicast.UserData.UdValue<global::System.Single>(Child("playtime_elapsed", true));
    ActiveMinutes = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("active_minutes", true));
    ActiveSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("active_seconds", true));
    FakePlaytimeMinutes = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("fake_playtime_minutes", true));
    SoundEnabled = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("sound_enabled", true), true);
    MusicEnabled = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("music_enabled", true), true);
    HapticsEnabled = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("haptics_enabled", true), true);
    HighQualityEnabled = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("high_quality_enabled", true), true);
    SoundVolume = new global::Multicast.UserData.UdValue<global::System.Single>(Child("sound_volume", true), 0.5f);
    MusicVolume = new global::Multicast.UserData.UdValue<global::System.Single>(Child("music_volume", true), 0.5f);
    RewardedWatchTimes = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("rewarded_watch_times", true));
    RewardedTryAttempt = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("rewarded_try_attempt", true));
    LoadingDelay = new global::Multicast.UserData.UdValue<global::System.Single>(Child("loading_delay", true));
    InAppsBoughtValueInRewarded = new global::Multicast.UserData.UdValue<global::System.Single>(Child("in_apps_bought_value_in_rewarded", true));
    RewardedDelayFromStartUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("rewarded_delay_from_start_unlocked", true));
    GpmIndex = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("gpm_index", true));
    PoolIndex = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("pool_index", true));
    OptimumTime = new global::Multicast.UserData.UdValue<global::System.Single>(Child("optimum_time", true));
    Impressions = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("impressions", true));
    AdRevenue = new global::Multicast.UserData.UdValue<global::System.Single>(Child("ad_revenue", true));
    Ecpm = new global::Multicast.UserData.UdValue<global::System.Single>(Child("ecpm", true));
    InstallTime = new global::Multicast.UserData.UdValue<global::System.Double>(Child("install_time", true));
    DaysSinceInstall = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("days_since_install", true));
    AppVersion = new global::Multicast.UserData.UdValue<global::System.String>(Child("app_version", true));
    FbAchievements = new global::Multicast.UserData.UdValue<global::System.String>(Child("fb_achievements", true));
    BannerImpressions = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("banner_impressions", true));
    BannerAdRevenue = new global::Multicast.UserData.UdValue<global::System.Single>(Child("banner_ad_revenue", true));
    BannerEcpm = new global::Multicast.UserData.UdValue<global::System.Single>(Child("banner_ecpm", true));
    CompletedRateUs = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("completed_rate_us", true));
    RewardedWeights = new global::Multicast.UserData.UdListObject<global::Codebase.Analytics.RewardedWeightUd>(Child("rewarded_weights", true), childArgs => new global::Codebase.Analytics.RewardedWeightUd(childArgs));
    FirstBuyPackRewardsGranted = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("first_buy_pack_rewards_granted", true));
    DailyAdsWatched = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("daily_ads_watched", true));
    DailyAdsResetTime = new global::Multicast.UserData.UdValue<global::System.Double>(Child("daily_ads_reset_time", true));
    NoAdsPurchased = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("no_ads_purchased", true));
    SoulPassPurchased = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("soul_offer_purchased", true));
    CrystalPassPurchased = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("crystal_offer_purchased", true));
    SoulPassPurchaseTime = new global::Multicast.UserData.UdValue<global::System.Double>(Child("soul_pass_purchase_time", true));
    CrystalPassPurchaseTime = new global::Multicast.UserData.UdValue<global::System.Double>(Child("crystal_pass_purchase_time", true));
    SoulPassLastDailyMailTime = new global::Multicast.UserData.UdValue<global::System.Double>(Child("soul_pass_last_daily_mail_time", true));
    ChaoticSoulOfferPurchased = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("chaotic_soul_offer_purchased", true));
    ChaoticSoulOfferPurchaseTime = new global::Multicast.UserData.UdValue<global::System.Double>(Child("chaotic_soul_offer_purchase_time", true));
    LocalPurchaseTransactions = new UdDictValue<string>(Child("local_purchase_transactions", true));
    FreeBoosterTokens = new global::Multicast.UserData.UdDictValue<global::System.Int32>(Child("free_booster_tokens", true));
}
	}
}
