using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using UniMob;
using UniMob.Core;

namespace Codebase.Events.Models
{
	public class EventModel : Model
	{
		private EventsUd data;

		private readonly List<EventBoosterModel> boosters;

		[CompilerGenerated]
		private int __SpawnerTick__BackingField;

		private ComputedAtom<EventDifficultySettings> __DifficultyDef;

		private ComputedAtom<EventState> __State;

		private ComputedAtom<bool> __IsReady;

		private ComputedAtom<bool> __IsActive;

		private ComputedAtom<bool> __IsCompleted;

		private ComputedAtom<bool> __IsClaimed;

		private ComputedAtom<EventDifficulty> __Difficulty;

		private ComputedAtom<int> __Progress;

		private ComputedAtom<int> __RosterMilestoneClaimedCount;

		private ComputedAtom<int> __RosterLeagueIndex;

		private ComputedAtom<int> __ProgressMax;

		private ComputedAtom<float> __ProgressNormalized;

		private ComputedAtom<bool> __IsProgressComplete;

		private ComputedAtom<float> __ExtraDuration;

		private ComputedAtom<DateTime?> __ActivatedAt;

		private ComputedAtom<int> __SpawnerTick;

		private ComputedAtom<bool> __IsDifficultySelected;

		public EventConfig Def { get; }

		public EventsUd Data => data ?? (data = UserDataProvider.Resolve<EventsUd>());

		public IReadOnlyList<EventBoosterModel> Boosters => boosters;

		public bool IsCrystalRosterEvent => Def.CrystalRoster != null;

		public string Key => Def.Key;

		public int RequiredLocationNumber => Def.RequiredLocationNumber;

		public EventDifficultySettings DifficultyDef
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14e7b24.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__DifficultyDef == null) __DifficultyDef = CodeGenAtom.CreatePooled(this, null, () => DifficultyDef, false);
                return __DifficultyDef.DirectEvaluate() ? Def.GetDifficultySettings(Difficulty) : __DifficultyDef.Value;
            }
        }

		public EventState State
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ed42c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__State == null) __State = CodeGenAtom.CreatePooled(this, null, () => State, false);
                return __State.DirectEvaluate() ? (EventState)Data.State.Value : __State.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14ed588.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__State != null && !__State.CompareAndInvalidate(value)) return;
                Data.State.Value = (int)value;
            }
        }

		public bool IsReady
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ed638.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsReady == null) __IsReady = CodeGenAtom.CreatePooled(this, null, () => IsReady, false);
                return __IsReady.DirectEvaluate() ? State == EventState.Ready : __IsReady.Value;
            }
        }

		public bool IsActive
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14e98e8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsActive == null) __IsActive = CodeGenAtom.CreatePooled(this, null, () => IsActive, false);
                return __IsActive.DirectEvaluate() ? State == EventState.Active : __IsActive.Value;
            }
        }

		public bool IsCompleted
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ed774.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsCompleted == null) __IsCompleted = CodeGenAtom.CreatePooled(this, null, () => IsCompleted, false);
                return __IsCompleted.DirectEvaluate() ? (State == EventState.Completed || State == EventState.Claimed) : __IsCompleted.Value;
            }
        }

		public bool IsClaimed
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ed8b4.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsClaimed == null) __IsClaimed = CodeGenAtom.CreatePooled(this, null, () => IsClaimed, false);
                return __IsClaimed.DirectEvaluate() ? State == EventState.Claimed : __IsClaimed.Value;
            }
        }

		public EventDifficulty Difficulty
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ed2d0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Difficulty == null) __Difficulty = CodeGenAtom.CreatePooled(this, null, () => Difficulty, false);
                return __Difficulty.DirectEvaluate() ? Data.Difficulty.Value : __Difficulty.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14ed9f0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Difficulty != null && !__Difficulty.CompareAndInvalidate(value)) return;
                Data.Difficulty.Value = value;
            }
        }

		public int Progress
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14edaa0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Progress == null) __Progress = CodeGenAtom.CreatePooled(this, null, () => Progress, false);
                return __Progress.DirectEvaluate() ? Data.Progress.Value : __Progress.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14edbfc.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Progress != null && !__Progress.CompareAndInvalidate(value)) return;
                Data.Progress.Value = value;
            }
        }

		public int RosterMilestoneClaimedCount
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14edcac.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__RosterMilestoneClaimedCount == null) __RosterMilestoneClaimedCount = CodeGenAtom.CreatePooled(this, null, () => RosterMilestoneClaimedCount, false);
                return __RosterMilestoneClaimedCount.DirectEvaluate() ? Data.RosterMilestoneClaimedCount.Value : __RosterMilestoneClaimedCount.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14ede08.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__RosterMilestoneClaimedCount != null && !__RosterMilestoneClaimedCount.CompareAndInvalidate(value)) return;
                Data.RosterMilestoneClaimedCount.Value = value;
            }
        }

		public int RosterLeagueIndex
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14e79c8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__RosterLeagueIndex == null) __RosterLeagueIndex = CodeGenAtom.CreatePooled(this, null, () => RosterLeagueIndex, false);
                return __RosterLeagueIndex.DirectEvaluate() ? Data.RosterLeagueIndex.Value : __RosterLeagueIndex.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14edeb8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__RosterLeagueIndex != null && !__RosterLeagueIndex.CompareAndInvalidate(value)) return;
                Data.RosterLeagueIndex.Value = value;
            }
        }

		public int ProgressMax
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14edf68.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ProgressMax == null) __ProgressMax = CodeGenAtom.CreatePooled(this, null, () => ProgressMax, false);
                return __ProgressMax.DirectEvaluate() ? (Key == "crystal_event_roster" ? Def.CrystalRoster.StagesCount : DifficultyDef != null ? DifficultyDef.StagesCount : 0) : __ProgressMax.Value;
            }
        }

		public float ProgressNormalized
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ee0f0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ProgressNormalized == null) __ProgressNormalized = CodeGenAtom.CreatePooled(this, null, () => ProgressNormalized, false);
                return __ProgressNormalized.DirectEvaluate() ? (ProgressMax <= 0 ? 0f : (float)Progress / ProgressMax) : __ProgressNormalized.Value;
            }
        }

		public bool IsProgressComplete
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ee24c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsProgressComplete == null) __IsProgressComplete = CodeGenAtom.CreatePooled(this, null, () => IsProgressComplete, false);
                return __IsProgressComplete.DirectEvaluate() ? Progress >= ProgressMax : __IsProgressComplete.Value;
            }
        }

		public float ExtraDuration
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14e978c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ExtraDuration == null) __ExtraDuration = CodeGenAtom.CreatePooled(this, null, () => ExtraDuration, false);
                return __ExtraDuration.DirectEvaluate() ? Data.ExtraDuration.Value : __ExtraDuration.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14ee394.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ExtraDuration != null && !__ExtraDuration.CompareAndInvalidate(value)) return;
                Data.ExtraDuration.Value = value;
            }
        }

		public DateTime? ActivatedAt
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14e9a24.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ActivatedAt == null) __ActivatedAt = CodeGenAtom.CreatePooled(this, null, () => ActivatedAt, false);
                return __ActivatedAt.DirectEvaluate() ? (Data.ActivatedAtSeconds.Value <= 0 ? (DateTime?)null : DateTimeOffset.FromUnixTimeSeconds(Data.ActivatedAtSeconds.Value).UtcDateTime) : __ActivatedAt.Value;
            }
            set
            {
                // Original APK ARM64 ELF VA 0x14ee44c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ActivatedAt != null && !__ActivatedAt.CompareAndInvalidate(value)) return;
                Data.ActivatedAtSeconds.Value = value.HasValue ? (int)new DateTimeOffset(value.Value.ToUniversalTime()).ToUnixTimeSeconds() : 0;
            }
        }

		public int DurationMinutes => (Key == "crystal_event_roster" ? Def.CrystalRoster.ForecastSettings.DurationMinutes : DifficultyDef != null ? DifficultyDef.DurationMinutes : 0);

		public int SpawnerTick
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ebf98.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SpawnerTick == null) __SpawnerTick = CodeGenAtom.CreatePooled(this, null, () => SpawnerTick, false);
                return __SpawnerTick.DirectEvaluate() ? __SpawnerTick__BackingField : __SpawnerTick.Value;
            }
            private set
            {
                // Original APK ARM64 ELF VA 0x14ee60c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SpawnerTick != null && !__SpawnerTick.CompareAndInvalidate(value)) return;
                __SpawnerTick__BackingField = value;
            }
        }

		public bool IsDifficultySelected
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eea44.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsDifficultySelected == null) __IsDifficultySelected = CodeGenAtom.CreatePooled(this, null, () => IsDifficultySelected, false);
                return __IsDifficultySelected.DirectEvaluate() ? (State == EventState.Ready && (Key == "crystal_event_roster" || Difficulty != (EventDifficulty)(-1))) : __IsDifficultySelected.Value;
            }
        }

		public bool MaxLeagueReached => RosterLeagueIndex >= Def.CrystalRoster.Leagues.Count - 1;

		public EventModel(Lifetime lifetime, EventConfig config)
			: base(lifetime)
		{
			// Original APK ARM64 ELF VA 0x14ecefc.
			Def = config;
			boosters = new List<EventBoosterModel>();
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x14ed014.
			base.Initialize();
			InitializeBoosters();
		}

		private void InitializeBoosters()
		{
			// Original APK ARM64 ELF VA 0x14ed030.
			if (Def.Boosters == null) return;
			foreach (EventBoosterDef def in Def.Boosters)
			{
			    var booster = new EventBoosterModel(Lifetime, def, this);
			    Injector.InjectInto(booster);
			    booster.Initialize();
			    boosters.Add(booster);
			}
		}

		public void RefreshSpawners()
		{
			// Original APK ARM64 ELF VA 0x14ee684.
			SpawnerTick++;
		}

		public double GetCurrency(string currencyKey)
		{
			// Original APK ARM64 ELF VA 0x14ee6a4.
			return Data.Currencies.TryGetValue(currencyKey, out double value) ? value : 0.0;
		}

		public double GetCollectedCurrency(string currencyKey)
		{
			// Original APK ARM64 ELF VA 0x14ee72c.
			return Data.CollectedCurrencies.TryGetValue(currencyKey, out double value) ? value : 0.0;
		}

		public void SetCurrency(string currencyKey, double value)
		{
			// Original APK ARM64 ELF VA 0x14ee7b4.
			Data.Currencies[currencyKey] = value;
		}

		public void AddCurrency(string currencyKey, double amount)
		{
			// Original APK ARM64 ELF VA 0x14ee830.
			Data.Currencies[currencyKey] = GetCurrency(currencyKey) + amount;
			Data.CollectedCurrencies[currencyKey] = GetCollectedCurrency(currencyKey) + amount;
		}

		public bool TrySpendCurrency(string currencyKey, double amount)
		{
			// Original APK ARM64 ELF VA 0x14ee904.
			double current = GetCurrency(currencyKey);
			if (!(amount <= current)) return false;
			Data.Currencies[currencyKey] = current - amount;
			return true;
		}

		public void AddProgress(int amount)
		{
			// Original APK ARM64 ELF VA 0x14ee9ac.
			Progress += amount;
			if (Progress > ProgressMax) Progress = ProgressMax;
		}

		public void ResetProgress()
		{
			// Original APK ARM64 ELF VA 0x14eea10.
			Progress = 0;
		}

		public void MakeReady()
		{
			// Original APK ARM64 ELF VA 0x14eea18.
			if (State == EventState.NotStarted || State == EventState.Ready) State = EventState.Ready;
		}

		public void Activate()
		{
			// Original APK ARM64 ELF VA 0x14eec38.
			if (State == EventState.Ready) State = EventState.Active;
		}

		public void Complete()
		{
			// Original APK ARM64 ELF VA 0x14eec64.
			if (State == EventState.Active) State = EventState.Completed;
		}

		public void Claim()
		{
			// Original APK ARM64 ELF VA 0x14eec90.
			if (State == EventState.Completed) State = EventState.Claimed;
		}

		public void Reset()
		{
			// Original APK ARM64 ELF VA 0x14eecbc.
			State = EventState.NotStarted;
			Difficulty = (EventDifficulty)(-1);
			Progress = 0;
			RosterMilestoneClaimedCount = 0;
			ExtraDuration = 0f;
			ActivatedAt = null;
			Data.Currencies.Clear();
			Data.CollectedCurrencies.Clear();
			Data.BoosterPurchases.Clear();
			ResetBakedRewards();
		}

		private void ResetBakedRewards()
		{
			// Original APK ARM64 ELF VA 0x14eedb8.
			if (Def.DifficultySettings == null) return;
			foreach (EventDifficultySettings def in Def.DifficultySettings)
			{
			    if (def.Rewards != null) ResetBakedRewardsForList(def.Rewards);
			    if (def.FailedRewards != null) ResetBakedRewardsForList(def.FailedRewards);
			}
		}

		private void ResetBakedRewardsForList(IEnumerable<RewardBase> rewards)
		{
			// Original APK ARM64 ELF VA 0x14eef94.
			foreach (RewardBase reward in rewards)
			    if (reward is StatReward stat && !string.IsNullOrEmpty(stat.SaveKey)) BakedRewardStorage.Remove(stat.SaveKey);
		}

		public bool TryAdvanceRosterLeague(ForecastEventPlan? plan)
		{
			// Original APK ARM64 ELF VA 0x14ef290.
			if (!plan.HasValue) return false;
			int nextLeague = plan.Value.LeagueIndex + 1;
			if (Progress < Def.CrystalRoster.Leagues[plan.Value.LeagueIndex].BossesToAdvance ||
			    nextLeague >= Def.CrystalRoster.Leagues.Count || RosterLeagueIndex >= nextLeague) return false;
			RosterLeagueIndex = nextLeague;
			return true;
		}

		public int GetBoosterPurchases(string boosterKey)
		{
			// Original APK ARM64 ELF VA 0x14eb490.
			return Data.BoosterPurchases.TryGetValue(boosterKey, out int value) ? value : 0;
		}

		public void IncrementBoosterPurchases(string boosterKey)
		{
			// Original APK ARM64 ELF VA 0x14ecb14.
			Data.BoosterPurchases[boosterKey] = GetBoosterPurchases(boosterKey) + 1;
		}

		public override void Dispose()
		{
			// Original APK ARM64 ELF VA 0x14ef3e8.
			base.Dispose();
			foreach (EventBoosterModel booster in boosters) booster.Dispose();
			boosters.Clear();
		}
	}
}
