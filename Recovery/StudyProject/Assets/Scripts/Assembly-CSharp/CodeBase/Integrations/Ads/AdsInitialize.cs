using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Analytics.Events;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Core.UserData;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Integrations.AppMetrica;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Player.Systems;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Codebase.Simulation.Systems;
using Codebase.UI.RewardedOffer;
using Codebase.Unit;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Integrations.Ads
{
	public class AdsInitialize : MonoBehaviour
	{
		private UserModel userModel;
		private AppMetricaAnalyticsWrapper analytics;
		[Inject] private CheatsModel cheatsModel;
		[Inject] private PlayerModel playerModel;
		[Inject] private FirebaseAnalyticsReporter firebaseAnalyticsReporter;
		[Inject] private ABTestService abTestService;
		[Inject] private EventsRepository eventsRepository;
		[Inject] private EventAnalyticsModel eventAnalyticsModel;
		[Inject] private UINavigator navigator;
		[Inject] private ITimeService timeService;
		private bool initialized;
		private float cooldown = 1f; // Native constructor 0x14c7b44; Start then sets 60.
		private float duration;
		private bool isShown;
		private bool shouldShowRewardedOffer;
		private string rewardedOfferStatKey;
		private double rewardedOfferValue;

		// Learning-project transport state; distinct from native isShown (offer visibility).
		private bool localAdShowing;
		[Header("学习工程：本地广告测试")]
		[SerializeField, Tooltip("跳过原版累计60分钟解锁及首次60秒等待。不会伪造存档中的解锁标记。")]
		private bool quickStartForStudy = true;
		[SerializeField, Min(0f)] private float studyInitialCooldown = 3f;
		public bool QuickStartForStudy => quickStartForStudy;
		public float Duration => duration;
		public float MaxDuration { get; private set; }
		public string RewardedAdUnitId => "f64a78ca5c7bcb51";

		// SDK-free adaptation: ready means the live game can consume a local completion.
		public bool IsRewardedAdReady => initialized && !localAdShowing && ServicesReady();

		private bool ServicesReady()
		{
			return userModel != null && playerModel != null &&
				!userModel.Lifetime.IsDisposed && !playerModel.Lifetime.IsDisposed &&
				userModel.Data != null && playerModel.Data != null && playerModel.Unit != null;
		}

		// Authorized local transport: one click completes one currently offered reward.
		// No MaxSdk, Adjust, ad identifier lookup, network or invented ad revenue.
		public bool TryShowRewardedAd()
		{
			if (!IsRewardedAdReady || !userModel.OfferedReward || duration <= 0f) return false;
			localAdShowing = true;
			bool completed = false;
			try
			{
				// Consume the transport request before callbacks can run user/UI reactions.
				// A partial reward failure must not make the same offer payable twice.
				userModel.OfferedReward = false;
				OnRewardedAdDisplayedEvent(RewardedAdUnitId, default);
				OnRewardedAdReceivedRewardEvent(RewardedAdUnitId, default, default);
				// Local completion does not lose SDK focus or pause the application.
				// Persist explicitly rather than depending on the original pause/autosave trigger.
				GameContext.Get<GameDataService>().ForceSave();
				completed = true;
				return true;
			}
			finally
			{
				try
				{
					if (!completed) ResetReward();
					OnRewardedAdHiddenEvent(RewardedAdUnitId, default);
				}
				finally { localAdShowing = false; }
			}
		}

		private void Awake()
		{
			// Native ELF VA 0x14c41b4.
			GameContext.Set(this);
		}

		private void Start()
		{
			// Native ELF VA 0x14c4224: preserve the original 60-second first cooldown.
			// Study-only first wait override; subsequent offer windows/cooldowns remain original.
			cooldown = quickStartForStudy ? Mathf.Max(0f, studyInitialCooldown) : 60f;
			userModel = GameContext.Get<UserModel>();
			analytics = GameContext.Get<AppMetricaAnalyticsWrapper>();
			// Original MAX callback subscriptions are replaced by the local call sequence above.
			MarkInitialized();
		}

		private void MarkInitialized()
		{
			// Native ELF VA 0x14c4528 depended on MAX initialization and Adjust.GetAdid.
			// Local readiness replaces only that transport/identifier integration.
			if (initialized) return;
			initialized = true;
		}

		private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Native ELF VA 0x14c464c.
			userModel.AdLoaded = true;
			userModel.Requested = false;
			userModel.RewardedTryAttempt.Value = 0;
			analytics.Send(new RewardedEvent("loaded", ""));
		}

		private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			// Native ELF VA 0x14c4748; ldexpf(1, min(6, attempt)). Not used by local success.
			userModel.RewardedTryAttempt.Value++;
			userModel.LoadingDelay.Value = Mathf.Pow(2f, Math.Min(6, userModel.RewardedTryAttempt.Value));
			userModel.Requested = false;
			analytics.Send(new RewardedEvent("load_failed", ""));
		}

		private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Native ELF VA 0x14c491c.
			analytics.Send(new RewardedEvent("displayed", userModel.CurrentRewardKey));
		}

		private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
		{
			// Native ELF VA 0x14c49c4. Retained compatible callback, no SDK invocation.
			userModel.AdLoaded = false;
			userModel.Requested = false;
			userModel.RewardedTryAttempt.Value = 0;
			userModel.LoadingDelay.Value = 1f;
			analytics.Send(new RewardedEvent("display_failed", ""));
			ResetReward();
		}

		private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Native ELF VA 0x14c4b34. This means an ad creative click, not the HUD offer click.
			analytics.Send(new RewardedEvent("clicked", userModel.CurrentRewardKey));
		}

		private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Native ELF VA 0x14c4bdc and original closure 0x14c7cfc.
			userModel.AdLoaded = false;
			userModel.Requested = false;
			userModel.RewardedTryAttempt.Value = 0;
			userModel.LoadingDelay.Value = 1f;
			analytics.Send(new RewardedEvent("hidden", ""));
			if (!shouldShowRewardedOffer) return;
			shouldShowRewardedOffer = false;
			var popup = navigator.Open<RewardedOfferPopupView>("Assets/Prefabs/UI/Rewarded/RewardedOffer.prefab", true);
			if (popup == null) return;
			string key = rewardedOfferStatKey;
			double value = rewardedOfferValue;
			popup.Setup(key, value);
			popup.SetUserCloseCallback(() => GrantAdReward(key, value, popup.TotalRewardValue));
		}

		private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
		{
			// Native ELF VA 0x14c4e84. Completion grants once, records daily/watch counts,
			// updates the selected dynamic weight, then consumes the visible offer.
			bool offerEnabled = abTestService.IsRewardedOfferEnabled();
			if (!offerEnabled) GrantAdReward();
			userModel.RewardedWatchTimes.Value++;
			userModel.IncrementDailyAds();
			var currentEvent = eventsRepository.CurrentEvent;
			if (currentEvent != null && currentEvent.IsActive) eventAnalyticsModel.IncrementAdsWatched(currentEvent.Key);
			analytics.Send(new RewardedEvent("watched", userModel.CurrentRewardKey));
			analytics.Send(new RewardedEvent("total", userModel.RewardedWatchTimes.Value.ToString()));
			analytics.Send(new OfferShownEvent(userModel.CurrentRewardKey, userModel.CurrentRewardValue, true, false, userModel.IsDoubleValue ? 2.0 : 1.0));
			if (abTestService.IsDynamicRewardedWeights())
			{
				var type = (StatType)Enum.Parse(typeof(StatType), userModel.CurrentRewardKey);
				var weights = GetCurrentWeights();
				UpdateRewardWeight(type, weights.TryGetValue(type, out float weight) ? weight + 1f : 2f);
				userModel.LastSkippedRewardKey = string.Empty;
			}
			if (offerEnabled)
			{
				shouldShowRewardedOffer = true;
				rewardedOfferStatKey = userModel.CurrentRewardKey;
				rewardedOfferValue = userModel.CurrentRewardValue;
			}
			ResetReward();
		}

		private void GrantAdReward()
		{
			// Native ELF VA 0x14c52bc.
			GrantAdReward(userModel.CurrentRewardKey, userModel.CurrentRewardValue);
		}

		private void GrantAdReward(string rewardKey, double rewardValue, double displayOverride = -1.0)
		{
			// Native ELF VA 0x14c56b8, predicate 0x14c7d28. Duration remains the native
			// constructor default 0: a saved permanent Flat stat reward, not a timed buff.
			Stat stat = GameContext.Get<PlayerModel>().Unit.Stats.First(value => value.Type.ToString() == rewardKey);
			GameContext.Get<PlayerRewardSystem>().GrantStatReward(new StatModifier
			{
				Name = "ads", ModifierType = ModifierType.Flat, Source = ModifierSource.Ads,
				StatType = stat.Type, Value = (float)rewardValue
			}, displayOverride, 1);
		}

		private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Deliberate SDK-free deviation from ELF 0x14c58c0: no ad was sold or shown,
			// so do not fabricate impressions/eCPM/revenue or send SDK payment telemetry.
		}

		private void Update()
		{
			// Native ELF VA 0x14c61ec. Only readiness/load transport is adapted locally.
			if (!initialized) MarkInitialized();
			if (!initialized || !ServicesReady()) return;
			duration -= Time.deltaTime;
			if (duration > 0f) return;
			if (isShown)
			{
				if (userModel.OfferedReward)
				{
					analytics.Send(new OfferShownEvent(userModel.CurrentRewardKey, userModel.CurrentRewardValue, false, true, userModel.IsDoubleValue ? 2.0 : 1.0));
					if (userModel.NoAdsPurchased.Value)
						analytics.Send(new RewardedNoAdsEvent("skipped", userModel.CurrentRewardKey, userModel.CurrentRewardValue));
					if (abTestService.IsDynamicRewardedWeights())
					{
						var type = (StatType)Enum.Parse(typeof(StatType), userModel.CurrentRewardKey);
						var weights = GetCurrentWeights();
						float current = weights.TryGetValue(type, out float weight) ? weight : 1f;
						UpdateRewardWeight(type, Mathf.Max(0.5f, Mathf.Pow(current, 0.9f) * 0.9f));
						userModel.LastSkippedRewardKey = userModel.CurrentRewardKey;
					}
				}
				isShown = false;
			}
			return;
		}

		public void OnAdClicked()
		{
			// Original ELF VA 0x14c741c is RET. The real offer entry is AdView.OnButtonClicked.
		}

		public void ResetAfterNoAdsReward()
		{
			// Original ELF VA 0x14c7420 is a tail B to ResetReward.
			ResetReward();
		}

		private void ResetReward()
		{
			// Native ELF VA 0x14c4b08.
			userModel.OfferedReward = false;
			duration = 0f;
			cooldown = GetAdCooldown();
		}

		private void StartCooldown()
		{
			// Native ELF VA 0x14c6bf0.
			cooldown = GetAdCooldown();
		}

		private float GetAdCooldown() => abTestService.GetNoAdsData().adCooldown;

		private WeightedReward GetRewardWithDynamicWeights()
		{
			// Native ELF VA 0x14c6c08; predicate 0x14c7c1c is RPM[type] > 0.
			RewardPool pool = GameContext.Get<RewardedConfig>().RewardChances[userModel.PoolIndex.Value];
			if (!abTestService.IsDynamicRewardedWeights()) return pool.GetRandomReward();
			var rewards = pool.Reward.Where(reward => SimulationSystem.Instance.RPMCalculatedValue[reward.Type] > 0.0).ToList();
			if (rewards.Count == 0) throw new InvalidOperationException("Reward pool is empty");
			var weights = GetCurrentWeights();
			StatType? skipped = string.IsNullOrEmpty(userModel.LastSkippedRewardKey)
				? (StatType?)null : (StatType)Enum.Parse(typeof(StatType), userModel.LastSkippedRewardKey);
			float total = 0f;
			foreach (WeightedReward reward in rewards)
			{
				if (skipped.HasValue && reward.Type == skipped.Value) continue;
				total += weights.TryGetValue(reward.Type, out float weight) ? weight : 1f;
			}
			float roll = UnityEngine.Random.Range(0f, total);
			float cumulative = 0f;
			foreach (WeightedReward reward in rewards)
			{
				if (skipped.HasValue && reward.Type == skipped.Value) continue;
				cumulative += weights.TryGetValue(reward.Type, out float weight) ? weight : 1f;
				if (roll <= cumulative) return reward;
			}
			return rewards[rewards.Count - 1];
		}

		private Dictionary<StatType, float> GetCurrentWeights()
		{
			// Native ELF VA 0x14c52dc. Duplicate saved keys keep the last value.
			var weights = new Dictionary<StatType, float>();
			foreach (RewardedWeightUd row in userModel.RewardedWeights)
				weights[(StatType)row.Type.Value] = row.Weight.Value;
			return weights;
		}

		private void SaveWeights(Dictionary<StatType, float> weights)
		{
			// Native ELF VA 0x14c744c: update the first matching saved row or append it.
			foreach (var pair in weights)
			{
				RewardedWeightUd row = userModel.RewardedWeights.FirstOrDefault(value => value.Type.Value == (int)pair.Key);
				if (row == null)
				{
					row = userModel.RewardedWeights.Create();
					row.Type.Value = (int)pair.Key;
				}
				row.Weight.Value = pair.Value;
			}
		}

		private void UpdateRewardWeight(StatType rewardType, float newWeight)
		{
			// Native ELF VA 0x14c5630.
			var weights = GetCurrentWeights();
			weights[rewardType] = newWeight;
			SaveWeights(weights);
		}
	}
}
