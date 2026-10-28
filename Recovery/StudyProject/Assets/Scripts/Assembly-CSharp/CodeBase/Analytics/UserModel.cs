using System;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Integrations.AppMetrica;
using Codebase.Services.Time;
using Multicast.UserData;
using UniMob;
using UnityEngine;
using Codebase.Unit;
using Codebase.Analytics.Events;

namespace Codebase.Analytics
{
	public class UserModel : Model<UserDataUd>
	{
		private const string UserIdChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		private const int UserIdLength = 12;
		[Inject]
		private AppMetricaAnalyticsWrapper analytics;
		[Inject]
		private AmuletConfig amuletConfig;
		[Inject]
		private CrystalConfig crystalConfig;
		[Inject]
		private ITimeService timeService;
		private const double SecondsPerDay = 86400.0;
		public UdValue<string> UserId
		{
			get
			{
				return Data.UserId;
			}
		}

		public UdValue<int> Minutes
		{
			get
			{
				return Data.PlaytimeMinutes;
			}
		}

		public UdValue<int> Seconds
		{
			get
			{
				return Data.PlaytimeSeconds;
			}
		}

		public UdValue<float> Elapsed
		{
			get
			{
				return Data.PlaytimeElapsed;
			}
		}

		public UdValue<int> ActiveSeconds
		{
			get
			{
				return Data.ActiveSeconds;
			}
		}

		public UdValue<int> ActiveMinutes
		{
			get
			{
				return Data.ActiveMinutes;
			}
		}

		public UdValue<int> FakePlaytimeMinutes
		{
			get
			{
				return Data.FakePlaytimeMinutes;
			}
		}

		public UdValue<bool> SoundEnabled
		{
			get
			{
				return Data.SoundEnabled;
			}
		}

		public UdValue<bool> MusicEnabled
		{
			get
			{
				return Data.MusicEnabled;
			}
		}

		public UdValue<bool> HighQualityEnabled
		{
			get
			{
				return Data.HighQualityEnabled;
			}
		}

		public UdValue<bool> HapticsEnabled
		{
			get
			{
				return Data.HapticsEnabled;
			}
		}

		public UdValue<float> SoundVolume
		{
			get
			{
				return Data.SoundVolume;
			}
		}

		public UdValue<float> MusicVolume
		{
			get
			{
				return Data.MusicVolume;
			}
		}

		public bool AdLoaded { get; set; }
		public bool Requested { get; set; }

		public UdValue<int> RewardedWatchTimes
		{
			get
			{
				return Data.RewardedWatchTimes;
			}
		}

		public UdValue<int> RewardedTryAttempt
		{
			get
			{
				return Data.RewardedTryAttempt;
			}
		}

		public UdValue<float> LoadingDelay
		{
			get
			{
				return Data.LoadingDelay;
			}
		}

		public UdValue<float> InAppsBoughtValueInRewarded
		{
			get
			{
				return Data.InAppsBoughtValueInRewarded;
			}
		}

		public UdValue<bool> RewardedDelayFromStartUnlocked
		{
			get
			{
				return Data.RewardedDelayFromStartUnlocked;
			}
		}

		public bool OfferedReward { get; set; }
		public string CurrentRewardKey { get; set; }
		public double CurrentRewardValue { get; set; }
		public bool IsDoubleValue { get; set; }
		public float RewardedDensity => GetRewardedDensity(0f);

		public UdValue<int> bannerImpressions
		{
			get
			{
				return Data.BannerImpressions;
			}
		}

		public UdValue<float> bannerAdRevenue
		{
			get
			{
				return Data.BannerAdRevenue;
			}
		}

		public UdValue<float> bannerEcpm
		{
			get
			{
				return Data.BannerEcpm;
			}
		}

		public UdValue<int> GpmIndex
		{
			get
			{
				return Data.GpmIndex;
			}
		}

		public UdValue<int> PoolIndex
		{
			get
			{
				return Data.PoolIndex;
			}
		}

		public UdValue<float> OptimumTime
		{
			get
			{
				return Data.OptimumTime;
			}
		}

		public UdValue<int> impressions
		{
			get
			{
				return Data.Impressions;
			}
		}

		public UdValue<float> adRevenue
		{
			get
			{
				return Data.AdRevenue;
			}
		}

		public UdValue<float> ecpm
		{
			get
			{
				return Data.Ecpm;
			}
		}

		public UdValue<double> installTime
		{
			get
			{
				return Data.InstallTime;
			}
		}

		public UdValue<int> daysSinceInstall
		{
			get
			{
				return Data.DaysSinceInstall;
			}
		}

		public UdValue<string> fbAchievements
		{
			get
			{
				return Data.FbAchievements;
			}
		}

		public string PreviousAppVersion { get; private set; }

		public UdValue<bool> FirstBuyPackRewardsGranted
		{
			get
			{
				return Data.FirstBuyPackRewardsGranted;
			}
		}

		public UdValue<int> DailyAdsWatched
		{
			get
			{
				return Data.DailyAdsWatched;
			}
		}

		public UdValue<double> DailyAdsResetTime
		{
			get
			{
				return Data.DailyAdsResetTime;
			}
		}

		public UdValue<bool> NoAdsPurchased
		{
			get
			{
				return Data.NoAdsPurchased;
			}
		}

		public UdValue<bool> SoulPassPurchased
		{
			get
			{
				return Data.SoulPassPurchased;
			}
		}

		public UdValue<bool> CrystalPassPurchased
		{
			get
			{
				return Data.CrystalPassPurchased;
			}
		}

		public UdValue<double> SoulPassPurchaseTime
		{
			get
			{
				return Data.SoulPassPurchaseTime;
			}
		}

		public UdValue<double> CrystalPassPurchaseTime
		{
			get
			{
				return Data.CrystalPassPurchaseTime;
			}
		}

		public UdValue<double> SoulPassLastDailyMailTime
		{
			get
			{
				return Data.SoulPassLastDailyMailTime;
			}
		}

		public UdValue<bool> ChaoticSoulOfferPurchased
		{
			get
			{
				return Data.ChaoticSoulOfferPurchased;
			}
		}

		public UdValue<double> ChaoticSoulOfferPurchaseTime
		{
			get
			{
				return Data.ChaoticSoulOfferPurchaseTime;
			}
		}

		// Native ELF0x15aa54c: preserve the existing native week-start helper and DateTimeOffset conversion.
        public double ChaoticSoulOfferNextResetTime => new DateTimeOffset(GetCurrentWeekStart().AddDays(7.0)).ToUnixTimeSeconds();

		public UdDictValue<int> FreeBoosterTokens
		{
			get
			{
				return Data.FreeBoosterTokens;
			}
		}

		public UdValue<bool> CompletedRateUs
		{
			get
			{
				return Data.CompletedRateUs;
			}
		}

		public float SkipSeconds { get; set; }

		public UdListObject<RewardedWeightUd> RewardedWeights
		{
			get
			{
				return Data.RewardedWeights;
			}
		}

		public string LastSkippedRewardKey { get; set; }

		public float GetRewardedDensity(float offsetMinutes)
		{
			// Original ARM64 ELF VA 0x15a9df8: reward equivalents per elapsed hour.
			float elapsedMinutes = Math.Max(1f, Minutes.Value - offsetMinutes);
			return (InAppsBoughtValueInRewarded.Value + RewardedWatchTimes.Value) / (elapsedMinutes / 60f);
		}

		public int GetFreeBoosterTokens(string productId)
		{
            // Native 0x15aa84c.
            return FreeBoosterTokens.TryGetValue(productId, out int count) ? count : 0;
        }

		public void AddFreeBoosterToken(string productId)
		{
            // Native 0x15aa8c8.
            FreeBoosterTokens[productId] = unchecked(GetFreeBoosterTokens(productId) + 1);
        }

		public bool ConsumeFreeBoosterToken(string productId)
		{
            // Native 0x15aa94c.
            int count = GetFreeBoosterTokens(productId);
            if (count <= 0) return false;
            int remaining = count - 1;
            if (remaining == 0) FreeBoosterTokens.Remove(productId);
            else FreeBoosterTokens[productId] = remaining;
            return true;
        }

		public void ResetDailyAdsIfNeeded()
		{
			// Original ARM64 ELF VA 0x15aaa0c uses real UTC midnight, not the game time offset.
			double utcMidnight = new DateTimeOffset((Codebase.Simulation.Harness.SimulationSandbox.Active ? Codebase.Simulation.Harness.SimulationSandbox.ClockUtcOffset : DateTimeOffset.UtcNow).Date, TimeSpan.Zero).ToUnixTimeSeconds();
			if (utcMidnight > DailyAdsResetTime.Value)
			{
				DailyAdsWatched.Value = 0;
				DailyAdsResetTime.Value = utcMidnight;
			}
		}

		public bool CanWatchAd(int maxAdsPerDay)
		{
			// Original ARM64 ELF VA 0x15aab90: nonpositive limits bypass the daily reset.
			if (maxAdsPerDay <= 0) return true;
			ResetDailyAdsIfNeeded();
			return DailyAdsWatched.Value < maxAdsPerDay;
		}

		public void IncrementDailyAds()
		{
			// Original ARM64 ELF VA 0x15aac1c.
			ResetDailyAdsIfNeeded();
			DailyAdsWatched.Value++;
		}

		public UserModel(Lifetime lifetime) : base(lifetime)
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			PreviousAppVersion = Data.AppVersion.Value;
			Data.AppVersion.Value = Application.version;
			if (installTime.Value <= 0.0)
				installTime.Value = (Codebase.Simulation.Harness.SimulationSandbox.Active ? Codebase.Simulation.Harness.SimulationSandbox.ClockUtcOffset : DateTimeOffset.UtcNow).ToUnixTimeSeconds();
			double elapsed = Math.Max(0.0, (Codebase.Simulation.Harness.SimulationSandbox.Active ? Codebase.Simulation.Harness.SimulationSandbox.ClockUtcOffset : DateTimeOffset.UtcNow).ToUnixTimeSeconds() - installTime.Value);
			int days = (int)TimeSpan.FromSeconds(elapsed).TotalDays;
			if (daysSinceInstall.Value != days)
				daysSinceInstall.Value = days;
			ExpirePassesIfNeeded();
			ResetWeeklyOffersIfNeeded();
			if (RewardedWeights.Count == 0)
			{
				foreach (StatType type in Enum.GetValues(typeof(StatType)))
				{
					var weight = RewardedWeights.Create();
					weight.Type.Value = (int)type;
					weight.Weight.Value = 1f;
				}
			}

			if (string.IsNullOrEmpty(UserId.Value))
			{
				UserId.Value = GenerateUserId();
				analytics.Send(new UserIdGeneratedEvent(UserId.Value));
			}
		}

		public void ExpirePassesIfNeeded()
		{
			double now = new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			if (SoulPassPurchased.Value && SoulPassPurchaseTime.Value > 0.0 && now - SoulPassPurchaseTime.Value >= amuletConfig.PassDurationDays * SecondsPerDay)
				SoulPassPurchased.Value = false;
			if (CrystalPassPurchased.Value && CrystalPassPurchaseTime.Value > 0.0 && now - CrystalPassPurchaseTime.Value >= crystalConfig.PassDurationDays * SecondsPerDay)
				CrystalPassPurchased.Value = false;
		}

		public void ResetWeeklyOffersIfNeeded()
		{
			double weekStart = new DateTimeOffset(GetCurrentWeekStart()).ToUnixTimeSeconds();
			if (ChaoticSoulOfferPurchased.Value && ChaoticSoulOfferPurchaseTime.Value < weekStart)
				ChaoticSoulOfferPurchased.Value = false;
		}

		private DateTime GetCurrentWeekStart()
		{
			DateTime now = timeService.Now.ToDateTime();
			return now.Date.AddDays(-(((int)now.DayOfWeek + 1) % 7));
		}

		private static string GenerateUserId()
		{
			var random = new System.Random();
			var characters = new char[UserIdLength];
			for (int i = 0; i < characters.Length; i++)
				characters[i] = UserIdChars[random.Next(UserIdChars.Length)];
			return new string (characters);
		}
	}
}
