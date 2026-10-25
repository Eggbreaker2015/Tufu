using System.Collections.Generic;
using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Integrations.AppMetrica;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Integrations.Ads
{
	public class AdsInitialize : MonoBehaviour
	{
		private UserModel userModel;

		private AppMetricaAnalyticsWrapper analytics;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private FirebaseAnalyticsReporter firebaseAnalyticsReporter;

		[Inject]
		private ABTestService abTestService;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private ITimeService timeService;

		private bool initialized;

		private float cooldown;

		private float duration;

		private bool isShown;

		private bool shouldShowRewardedOffer;

		private string rewardedOfferStatKey;

		private double rewardedOfferValue;

		public float Duration => this.duration;

		public float MaxDuration { get; private set; }

		public string RewardedAdUnitId => "f64a78ca5c7bcb51"; // Original native14c4174.

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<AdsInitialize>(this);
        }

		private void Start()
		{
		}

		private void MarkInitialized()
		{
		}

		private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
		}

		private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void GrantAdReward()
		{
		}

		private void GrantAdReward(string rewardKey, double rewardValue, double displayOverride = -1.0)
		{
		}

		private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
		}

		private void Update()
		{
		}

		public void OnAdClicked()
		{
		}

		public void ResetAfterNoAdsReward()
		{
            // Native ELF0x14c7420.
            ResetReward();
        }

		private void ResetReward()
		{
            // Native ELF0x14c4b08.
            userModel.OfferedReward = false; duration = 0f; cooldown = GetAdCooldown();
        }

		private void StartCooldown()
		{
            // Native ELF0x14c6bf0.
            cooldown = GetAdCooldown();
        }

		private float GetAdCooldown()
		{
            // Native ELF0x14c7424.
            return abTestService.GetNoAdsData().adCooldown;
        }

		private WeightedReward GetRewardWithDynamicWeights()
		{
			return null;
		}

		private Dictionary<StatType, float> GetCurrentWeights()
		{
			return null;
		}

		private void SaveWeights(Dictionary<StatType, float> weights)
		{
		}

		private void UpdateRewardWeight(StatType rewardType, float newWeight)
		{
		}
	}
}
