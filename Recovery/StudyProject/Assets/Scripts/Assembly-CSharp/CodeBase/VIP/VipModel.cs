using System;
using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Services.Time;
using UniMob;
using UniMob.Core;
using System.Linq;

namespace Codebase.VIP
{
	public class VipModel : Model<VipUd>
	{
		[Inject]
		private VipConfig config;
		[Inject]
		private UserModel userModel;
		[Inject]
		private ITimeService timeService;
		private int lastNotifiedLevel;
		private Dictionary<string, List<IVipFeature>> cachedFeatures;
		private ComputedAtom<int> __Experience;
		private ComputedAtom<int> __Level;
		private ComputedAtom<int> __CurrentLevelExperience;
		private ComputedAtom<int> __NextLevelExperience;
		private ComputedAtom<float> __Progress;
		private ComputedAtom<bool> __IsMaxLevel;
		private ComputedAtom<TimeSpan> __DailyChestCooldownRemaining;
		public int Experience
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Experience == null)
					__Experience = CodeGenAtom.CreatePooled(this, null, () => Experience, false);
				if (!__Experience.DirectEvaluate())
					return __Experience.Value;
				return (int)userModel.InAppsBoughtValueInRewarded.Value;
			}
		}

		public int Level
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Level == null)
					__Level = CodeGenAtom.CreatePooled(this, null, () => Level, false);
				if (!__Level.DirectEvaluate())
					return __Level.Value;
				return CalculateLevel();
			}
		}

		public int CurrentLevelExperience
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CurrentLevelExperience == null)
					__CurrentLevelExperience = CodeGenAtom.CreatePooled(this, null, () => CurrentLevelExperience, false);
				if (!__CurrentLevelExperience.DirectEvaluate())
					return __CurrentLevelExperience.Value;
				return GetCurrentLevelExperience();
			}
		}

		public int NextLevelExperience
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__NextLevelExperience == null)
					__NextLevelExperience = CodeGenAtom.CreatePooled(this, null, () => NextLevelExperience, false);
				if (!__NextLevelExperience.DirectEvaluate())
					return __NextLevelExperience.Value;
				return config.GetRequiredExperience(unchecked(Level + 1));
			}
		}

		public float Progress
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Progress == null)
					__Progress = CodeGenAtom.CreatePooled(this, null, () => Progress, false);
				if (!__Progress.DirectEvaluate())
					return __Progress.Value;
				return IsMaxLevel ? 1f : NextLevelExperience > 0 ? (float)CurrentLevelExperience / NextLevelExperience : 0f;
			}
		}

		public bool IsMaxLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsMaxLevel == null)
					__IsMaxLevel = CodeGenAtom.CreatePooled(this, null, () => IsMaxLevel, false);
				if (!__IsMaxLevel.DirectEvaluate())
					return __IsMaxLevel.Value;
				return Level >= config.MaxLevel;
			}
		}

		public TimeSpan DailyChestCooldownRemaining
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DailyChestCooldownRemaining == null)
					__DailyChestCooldownRemaining = CodeGenAtom.CreatePooled(this, null, () => DailyChestCooldownRemaining, false);
				if (!__DailyChestCooldownRemaining.DirectEvaluate())
					return __DailyChestCooldownRemaining.Value;
				int timestamp = Data.DailyChestCooldownTimestamp.Value;
				if (timestamp <= 0)
					return TimeSpan.Zero;
				int now = unchecked((int)new DateTimeOffset(timeService.Now.ToDateTime().ToUniversalTime()).ToUnixTimeSeconds());
				int remaining = unchecked(timestamp - now);
				return remaining > 0 ? TimeSpan.FromSeconds(remaining) : TimeSpan.Zero;
			}
		}

		public VipModel(Lifetime lifetime) : base(lifetime)
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			lastNotifiedLevel = Level;
			InitializeFeatureCache();
			Atom.Reaction(Lifetime, () => Level, OnLevelChanged);
		}

		private void InitializeFeatureCache()
		{
			cachedFeatures = new Dictionary<string, List<IVipFeature>>();
			foreach (var featureConfig in config.Features)
			{
				IVipFeature feature = featureConfig.CreateFeature();
				string key = feature.Key;
				if (!cachedFeatures.ContainsKey(key))
					cachedFeatures[key] = new List<IVipFeature>();
				cachedFeatures[key].Add(feature);
			}
		}

		private void OnLevelChanged(int newLevel)
		{
			if (cachedFeatures == null)
				return;
			foreach (var list in cachedFeatures.Values)
				foreach (var feature in list)
					if (feature.IsUnlocked(newLevel))
						feature.OnLevelUp(lastNotifiedLevel, newLevel);
		}

		private int CalculateLevel()
		{
			if (Experience == 0)
				return 0;
			int result = 0;
			for (int level = 1; level <= config.MaxLevel; level++)
			{
				if (Experience < config.GetRequiredExperience(level))
					break;
				result = level;
			}

			return result;
		}

		private int GetCurrentLevelExperience()
		{
			return Level == 0 ? Experience : unchecked(Experience - config.GetRequiredExperience(Level));
		}

		public bool IsFeatureUnlocked(string featureKey)
		{
			return cachedFeatures != null && cachedFeatures.TryGetValue(featureKey, out var features) && features.Any(feature => feature.IsUnlocked(Level));
		}

		public T GetFeature<T>(string featureKey)
			where T : class, IVipFeature
		{
			if (cachedFeatures == null || !cachedFeatures.TryGetValue(featureKey, out var list))
				return null;
			T selected = null;
			int highest = -1;
			foreach (var feature in list)
				if (feature is T candidate && feature.IsUnlocked(Level) && feature.UnlockLevel > highest)
				{
					highest = feature.UnlockLevel;
					selected = candidate;
				}

			return selected;
		}

		public List<T> GetAllFeatures<T>(string featureKey)
			where T : class, IVipFeature
		{
			var result = new List<T>();
			if (cachedFeatures != null && cachedFeatures.TryGetValue(featureKey, out var list))
				foreach (var feature in list)
					if (feature is T candidate && feature.IsUnlocked(Level))
						result.Add(candidate);
			return result;
		}

		public List<IVipFeature> GetAllUnlockedFeatures()
		{
			var result = new List<IVipFeature>();
			if (cachedFeatures != null)
				foreach (var list in cachedFeatures.Values)
					foreach (var feature in list)
						if (feature.IsUnlocked(Level))
							result.Add(feature);
			return result;
		}

		public int GetDailyChestCooldownTimestamp()
		{
			return Data.DailyChestCooldownTimestamp.Value;
		}

		public void SetDailyChestCooldownTimestamp(int timestamp)
		{
			Data.DailyChestCooldownTimestamp.Value = timestamp;
		}

		public int GetAutoFarmCooldownTimestamp()
		{
			return Data.AutoFarmCooldownTimestamp.Value;
		}

		public void SetAutoFarmCooldownTimestamp(int timestamp)
		{
			Data.AutoFarmCooldownTimestamp.Value = timestamp;
		}

		public bool HasLeveledUp()
		{
			bool result = Level > lastNotifiedLevel;
			if (result)
				lastNotifiedLevel = Level;
			return result;
		}
	}
}
