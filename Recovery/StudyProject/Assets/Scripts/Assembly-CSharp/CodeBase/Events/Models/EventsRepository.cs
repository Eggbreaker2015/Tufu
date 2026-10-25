using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Events.Analytics;
using Codebase.Events.Services;
using Codebase.Player.Models;
using Codebase.Services.Time;
using Codebase.Simulation.Forecast;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using JetBrains.Annotations;
using UniMob;
using UniMob.Core;

namespace Codebase.Events.Models
{
	public class EventsRepository : Model
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildAndStoreEventPlanAsync_003Ed__58 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventModel model;

			public EventsRepository _003C_003E4__this;

			private UniTask<string>.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildForecastAndBakeRewardsIfNeededAsync_003Ed__55 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventsRepository _003C_003E4__this;

			public string key;

			public EventDifficulty difficulty;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildRosterEventPlanAsync_003Ed__56 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventsRepository _003C_003E4__this;

			public string key;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CStartEventAsync_003Ed__57 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventsRepository _003C_003E4__this;

			public string key;

			private EventModel _003Cmodel_003E5__2;

			private bool _003CisCrystalRoster_003E5__3;

			private EventDifficultySettings _003CdifficultySettings_003E5__4;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[Inject]
		private ITimeService timeService;

		[Inject]
		private EventsProvider eventsProvider;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		[Inject]
		private EnemiesModel enemiesModel;

		private readonly EventTimeCalculator timeCalculator;

		private readonly EventSlotManager slotManager;

		private readonly EventForecastService forecastService;

		private readonly EventDifficultyConfigurator difficultyConfigurator;

		private readonly List<EventModel> events;

		private ComputedAtom<bool> __HasEventPlan;

		private ComputedAtom<ForecastEventPlan?> __EventPlan;

		private ComputedAtom<bool> __HasReadyEvent;

		private ComputedAtom<bool> __HasActiveEvent;

		private ComputedAtom<bool> __HasCompletedEvent;

		private ComputedAtom<EventModel> __CompletedEvent;

		private ComputedAtom<bool> __HasAnyUnlockedEvent;

		private ComputedAtom<EventModel> __ReadyEvent;

		private ComputedAtom<EventModel> __CurrentEvent;

		private ComputedAtom<EventModel> __ScheduledEvent;

		private ComputedAtom<bool> __HasScheduledEvent;

		private ComputedAtom<TimeSpan> __TimeUntilScheduledEventEnds;

		private ComputedAtom<EventModel> __NextEvent;

		private ComputedAtom<TimeSpan> __TimeUntilNextEvent;

		public IReadOnlyList<EventModel> Events => null;

		public bool HasEventPlan => false;

		[CanBeNull]
		public ForecastEventPlan? EventPlan => null;

		public bool HasReadyEvent => false;

		public bool HasActiveEvent => false;

		public bool HasCompletedEvent => false;

		[CanBeNull]
		public EventModel CompletedEvent => null;

		public bool HasAnyUnlockedEvent => false;

		[CanBeNull]
		public EventModel ReadyEvent => null;

		[CanBeNull]
		public EventModel CurrentEvent => null;

		[CanBeNull]
		public EventModel ScheduledEvent => null;

		public bool HasScheduledEvent => false;

		public TimeSpan TimeUntilScheduledEventEnds => default;

		public EventModel NextEvent => null;

		public TimeSpan TimeUntilNextEvent => default;

		public EventsRepository(Lifetime lifetime)
			: base(default)
		{
		}

		public override void Initialize()
		{
		}

		public void Populate()
		{
		}

		public EventModel Get(string key)
		{
			return null;
		}

		public ForecastEventEnemyInfo? GetEventEnemyInfo(EventUnitType unitType)
		{
			return null;
		}

		public ForecastEventRewardInfo? GetEventRewardInfo(EventUnitType unitType)
		{
			return null;
		}

		public (int, int) GetEventMobCounts(EventModel eventModel)
		{
			return default;
		}

		public ForecastCrystalMilestonePlan? GetNextRosterMilestone(EventModel eventModel)
		{
			return null;
		}

		public bool HasClaimedAllRosterMilestones(EventModel eventModel)
		{
			return false;
		}

		public bool IsRosterMilestoneChestReady(EventModel eventModel)
		{
			return false;
		}

		public bool IsEventUnlocked(EventModel eventModel)
		{
			return false;
		}

		public bool IsCurrentEventSlotLocked(EventModel eventModel, DateTime nowUtc)
		{
			return false;
		}

		public bool MakeEventAvailable(string key)
		{
			return false;
		}

		public void PrepareEvent(string key, EventDifficulty difficulty)
		{
		}

		public bool ShouldRecalculateRewards(string key, EventDifficulty difficulty)
		{
			return false;
		}

		[AsyncStateMachine(typeof(_003CBuildForecastAndBakeRewardsIfNeededAsync_003Ed__55))]
		public UniTask BuildForecastAndBakeRewardsIfNeededAsync(string key, EventDifficulty difficulty)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CBuildRosterEventPlanAsync_003Ed__56))]
		public UniTask BuildRosterEventPlanAsync(string key)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CStartEventAsync_003Ed__57))]
		public UniTask StartEventAsync(string key)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CBuildAndStoreEventPlanAsync_003Ed__58))]
		private UniTask BuildAndStoreEventPlanAsync(EventModel model)
		{
			return default;
		}

		private void SpawnInitialEventMobs(EventModel model, int initialMobSpawnCount)
		{
		}

		public void ResetEvent(string key)
		{
		}

		public void ClearLastPlayedSlot()
		{
		}

		private void ResetEventSpawners()
		{
		}

		private void ResetEventSpawnersForKey(string eventKey)
		{
		}

		public void ClaimEvent(string key)
		{
		}
	}
}
