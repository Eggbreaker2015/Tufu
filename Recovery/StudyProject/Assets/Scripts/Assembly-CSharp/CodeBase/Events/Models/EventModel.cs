using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
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

		public EventsUd Data => null;

		public IReadOnlyList<EventBoosterModel> Boosters => null;

		public bool IsCrystalRosterEvent => false;

		public string Key => null;

		public int RequiredLocationNumber => 0;

		public EventDifficultySettings DifficultyDef => null;

		public EventState State
		{
			get
			{
				return EventState.NotStarted;
			}
			set
			{
			}
		}

		public bool IsReady => false;

		public bool IsActive => false;

		public bool IsCompleted => false;

		public bool IsClaimed => false;

		public EventDifficulty Difficulty
		{
			get
			{
				return EventDifficulty.Easy;
			}
			set
			{
			}
		}

		public int Progress
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int RosterMilestoneClaimedCount
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int RosterLeagueIndex
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int ProgressMax => 0;

		public float ProgressNormalized => 0f;

		public bool IsProgressComplete => false;

		public float ExtraDuration
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		public DateTime? ActivatedAt
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public int DurationMinutes => 0;

		public int SpawnerTick
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public bool IsDifficultySelected => false;

		public bool MaxLeagueReached => false;

		public EventModel(Lifetime lifetime, EventConfig config)
			: base(default)
		{
		}

		public override void Initialize()
		{
		}

		private void InitializeBoosters()
		{
		}

		public void RefreshSpawners()
		{
		}

		public double GetCurrency(string currencyKey)
		{
			return 0.0;
		}

		public double GetCollectedCurrency(string currencyKey)
		{
			return 0.0;
		}

		public void SetCurrency(string currencyKey, double value)
		{
		}

		public void AddCurrency(string currencyKey, double amount)
		{
		}

		public bool TrySpendCurrency(string currencyKey, double amount)
		{
			return false;
		}

		public void AddProgress(int amount)
		{
		}

		public void ResetProgress()
		{
		}

		public void MakeReady()
		{
		}

		public void Activate()
		{
		}

		public void Complete()
		{
		}

		public void Claim()
		{
		}

		public void Reset()
		{
		}

		private void ResetBakedRewards()
		{
		}

		private void ResetBakedRewardsForList(IEnumerable<RewardBase> rewards)
		{
		}

		public bool TryAdvanceRosterLeague(ForecastEventPlan? plan)
		{
			return false;
		}

		public int GetBoosterPurchases(string boosterKey)
		{
			return 0;
		}

		public void IncrementBoosterPurchases(string boosterKey)
		{
		}

		public override void Dispose()
		{
		}
	}
}
