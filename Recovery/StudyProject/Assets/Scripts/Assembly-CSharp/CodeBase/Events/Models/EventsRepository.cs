using System.Linq;
using Codebase.Core.UserData;
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

		public IReadOnlyList<EventModel> Events => events;

		public bool HasEventPlan
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eff68.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasEventPlan == null) __HasEventPlan = CodeGenAtom.CreatePooled(this, null, () => HasEventPlan, false);
                return __HasEventPlan.DirectEvaluate() ? forecastService.HasEventPlan : __HasEventPlan.Value;
            }
        }

		[CanBeNull]
		public ForecastEventPlan? EventPlan
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f00a4.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__EventPlan == null) __EventPlan = CodeGenAtom.CreatePooled(this, null, () => EventPlan, false);
                return __EventPlan.DirectEvaluate() ? forecastService.EventPlan : __EventPlan.Value;
            }
        }

		public bool HasReadyEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f0b50.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasReadyEvent == null) __HasReadyEvent = CodeGenAtom.CreatePooled(this, null, () => HasReadyEvent, false);
                return __HasReadyEvent.DirectEvaluate() ? events.Any(item => item.IsReady) : __HasReadyEvent.Value;
            }
        }

		public bool HasActiveEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f0d48.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasActiveEvent == null) __HasActiveEvent = CodeGenAtom.CreatePooled(this, null, () => HasActiveEvent, false);
                return __HasActiveEvent.DirectEvaluate() ? events.Any(item => item.IsActive) : __HasActiveEvent.Value;
            }
        }

		public bool HasCompletedEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f0f40.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasCompletedEvent == null) __HasCompletedEvent = CodeGenAtom.CreatePooled(this, null, () => HasCompletedEvent, false);
                return __HasCompletedEvent.DirectEvaluate() ? events.Any(item => item.State == EventState.Completed) : __HasCompletedEvent.Value;
            }
        }

		[CanBeNull]
		public EventModel CompletedEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f1138.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CompletedEvent == null) __CompletedEvent = CodeGenAtom.CreatePooled(this, null, () => CompletedEvent, false);
                return __CompletedEvent.DirectEvaluate() ? (HasCompletedEvent ? events.FirstOrDefault(item => item.State == EventState.Completed) : null) : __CompletedEvent.Value;
            }
        }

		public bool HasAnyUnlockedEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f1340.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasAnyUnlockedEvent == null) __HasAnyUnlockedEvent = CodeGenAtom.CreatePooled(this, null, () => HasAnyUnlockedEvent, false);
                return __HasAnyUnlockedEvent.DirectEvaluate() ? events.Any(IsEventUnlocked) : __HasAnyUnlockedEvent.Value;
            }
        }

		[CanBeNull]
		public EventModel ReadyEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f14d8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ReadyEvent == null) __ReadyEvent = CodeGenAtom.CreatePooled(this, null, () => ReadyEvent, false);
                return __ReadyEvent.DirectEvaluate() ? (HasReadyEvent ? events.FirstOrDefault(item => item.IsReady) : null) : __ReadyEvent.Value;
            }
        }

		[CanBeNull]
		public EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ecba8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? (HasActiveEvent ? events.FirstOrDefault(item => item.IsActive) : null) : __CurrentEvent.Value;
            }
        }

		[CanBeNull]
		public EventModel ScheduledEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f16e0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ScheduledEvent == null) __ScheduledEvent = CodeGenAtom.CreatePooled(this, null, () => ScheduledEvent, false);
                return __ScheduledEvent.DirectEvaluate() ? (ReadyEvent ?? CurrentEvent) : __ScheduledEvent.Value;
            }
        }

		public bool HasScheduledEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f1824.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasScheduledEvent == null) __HasScheduledEvent = CodeGenAtom.CreatePooled(this, null, () => HasScheduledEvent, false);
                return __HasScheduledEvent.DirectEvaluate() ? ScheduledEvent != null : __HasScheduledEvent.Value;
            }
        }

		public TimeSpan TimeUntilScheduledEventEnds
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f19a0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__TimeUntilScheduledEventEnds == null) __TimeUntilScheduledEventEnds = CodeGenAtom.CreatePooled(this, null, () => TimeUntilScheduledEventEnds, false);
                return __TimeUntilScheduledEventEnds.DirectEvaluate() ? (ScheduledEvent != null ? timeCalculator.GetTimeUntilEventEnds(ScheduledEvent) : TimeSpan.Zero) : __TimeUntilScheduledEventEnds.Value;
            }
        }

		public EventModel NextEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f1b1c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__NextEvent == null) __NextEvent = CodeGenAtom.CreatePooled(this, null, () => NextEvent, false);
                return __NextEvent.DirectEvaluate() ? timeCalculator.FindNextEvent(IsEventUnlocked).Item1 : __NextEvent.Value;
            }
        }

		public TimeSpan TimeUntilNextEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14f1c9c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__TimeUntilNextEvent == null) __TimeUntilNextEvent = CodeGenAtom.CreatePooled(this, null, () => TimeUntilNextEvent, false);
                return __TimeUntilNextEvent.DirectEvaluate() ? timeCalculator.GetTimeUntilNextEvent(IsEventUnlocked) : __TimeUntilNextEvent.Value;
            }
        }

		public EventsRepository(Lifetime lifetime)
			: base(lifetime)
		{
			// Original APK ARM64 ELF VA 0x14ef7ac.
			events = new List<EventModel>();
			slotManager = new EventSlotManager();
			timeCalculator = new EventTimeCalculator();
			forecastService = new EventForecastService();
			difficultyConfigurator = new EventDifficultyConfigurator();
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x14ef92c.
			base.Initialize();
			Injector.InjectInto(timeCalculator);
			timeCalculator.Initialize(events, slotManager.IsSlotLocked);
		}

		public void Populate()
		{
			// Original APK ARM64 ELF VA 0x14efa18.
			foreach (EventConfig config in eventsProvider.GetAll())
			{
			    var model = new EventModel(Lifetime, config);
			    Injector.InjectInto(model);
			    model.Initialize();
			    events.Add(model);
			}
		}

		public EventModel Get(string key)
		{
			// Original APK ARM64 ELF VA 0x14efd88.
			return events.FirstOrDefault(item => item.Key == key);
		}

		public ForecastEventEnemyInfo? GetEventEnemyInfo(EventUnitType unitType)
		{
			// Original APK ARM64 ELF VA 0x14f01f4.
			return forecastService.GetEventEnemyInfo(unitType, CurrentEvent);
		}

		public ForecastEventRewardInfo? GetEventRewardInfo(EventUnitType unitType)
		{
			// Original APK ARM64 ELF VA 0x14f0250.
			return forecastService.GetEventRewardInfo(unitType, CurrentEvent);
		}

		public (int, int) GetEventMobCounts(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14f02a4.
			int current = 0, maximum = 0;
			foreach (EnemySpawner spawner in enemiesModel.Spawners)
			{
			    if (!spawner.Config.IsEvent || spawner.Config.EventDef == null ||
			        spawner.Config.EventDef.Key != eventModel.Def.Key || spawner.Config.EventDef.UnitType != EventUnitType.Mob) continue;
			    current += spawner.CurrentCount;
			    maximum += spawner.MaxCount;
			}
			return (current, maximum);
		}

		public ForecastCrystalMilestonePlan? GetNextRosterMilestone(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14f0560.
			var plan = EventPlan;
			if (!plan.HasValue || plan.Value.CrystalMilestones == null || plan.Value.CrystalMilestones.Count == 0) return null;
			var milestones = plan.Value.CrystalMilestones;
			return milestones[Math.Clamp(eventModel.RosterMilestoneClaimedCount, 0, milestones.Count - 1)];
		}

		public bool HasClaimedAllRosterMilestones(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14f07dc.
			var plan = EventPlan;
			return plan.HasValue && plan.Value.CrystalMilestones != null && plan.Value.CrystalMilestones.Count > 0 &&
			    eventModel.RosterMilestoneClaimedCount >= plan.Value.CrystalMilestones.Count;
		}

		public bool IsRosterMilestoneChestReady(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14f0954.
			var plan = EventPlan;
			if (!plan.HasValue || plan.Value.CrystalMilestones == null || eventModel.RosterMilestoneClaimedCount >= plan.Value.CrystalMilestones.Count) return false;
			var milestone = plan.Value.CrystalMilestones[eventModel.RosterMilestoneClaimedCount];
			return eventModel.GetCollectedCurrency("red_crystal") + eventModel.GetCollectedCurrency("blue_crystal") >= milestone.RequiredCrystals;
		}

		public bool IsEventUnlocked(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14f195c.
			return playerModel.LastUnlockedLocation.Number >= eventModel.Def.RequiredLocationNumber;
		}

		public bool IsCurrentEventSlotLocked(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14f1e20.
			return slotManager.IsCurrentEventSlotLocked(eventModel, nowUtc);
		}

		public bool MakeEventAvailable(string key)
		{
			// Original APK ARM64 ELF VA 0x14f1e34.
			var model = Get(key);
			if (model == null || model.State != EventState.NotStarted) return false;
			model.MakeReady();
			return true;
		}

		public void PrepareEvent(string key, EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14f1e68.
			difficultyConfigurator.PrepareEvent(Get(key), difficulty);
		}

		public bool ShouldRecalculateRewards(string key, EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14f1eb0.
			return forecastService.ShouldRecalculateRewards(Get(key), difficulty);
		}

		public async UniTask BuildForecastAndBakeRewardsIfNeededAsync(string key, EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14f3210.
			await forecastService.BuildForecastAndBakeRewardsIfNeededAsync(Get(key), difficulty);
		}

		public async UniTask BuildRosterEventPlanAsync(string key)
		{
			// Original APK ARM64 ELF VA 0x14f35f0.
			var model = Get(key);
			if (model == null || model.Def == null || model.Def.CrystalRoster == null) return;
			model.Data.WeaponType.Value = playerModel.LastUnlockedLocation.PreferredWeaponType;
			model.Data.ResistType.Value = playerModel.LastUnlockedLocation.PreferredArmorType;
			if (forecastService.TryApplyCrystalStages(model)) return;
			await BuildAndStoreEventPlanAsync(model);
		}

		public async UniTask StartEventAsync(string key)
		{
			// Original APK ARM64 ELF VA 0x14f3af0.
			var model = Get(key);
			if (model == null) return;
			bool isCrystalRoster = model.Key == "crystal_event_roster" && model.Def.CrystalRoster != null;
			var difficulty = model.DifficultyDef;
			if (!isCrystalRoster && difficulty == null) return;
			if (isCrystalRoster)
			{
			    model.Data.WeaponType.Value = playerModel.LastUnlockedLocation.PreferredWeaponType;
			    model.Data.ResistType.Value = playerModel.LastUnlockedLocation.PreferredArmorType;
			}
			model.ActivatedAt = timeService.Now.ToDateTime().ToUniversalTime();
			await BuildAndStoreEventPlanAsync(model);
			model.Activate();
			slotManager.LockCurrentEventSlot(model, model.ActivatedAt.Value);
			eventAnalyticsModel?.OnEventStarted(model);
			ResetEventSpawnersForKey(key);
			SpawnInitialEventMobs(model, isCrystalRoster ? model.Def.CrystalRoster.ForecastSettings.InitialMobSpawnCount : difficulty?.InitialMobSpawnCount ?? 0);
		}

		private async UniTask BuildAndStoreEventPlanAsync(EventModel model)
		{
			// Original APK ARM64 ELF VA 0x14f2e40.
			if (model.Key == "crystal_event_roster" && forecastService.TryApplyCrystalStages(model)) return;
			string snapshot = await forecastService.BuildEventPlanAsync(model);
			if (snapshot != null) forecastService.SnapshotDataValue = snapshot;
		}

		private void SpawnInitialEventMobs(EventModel model, int initialMobSpawnCount)
		{
			// Original APK ARM64 ELF VA 0x14f21cc.
			if (initialMobSpawnCount <= 0) return;
			var spawners = enemiesModel.EventSpawners.Where(spawner => spawner.Config.EventDef != null &&
			    spawner.Config.EventDef.Key == model.Def.Key && spawner.Config.EventDef.UnitType == EventUnitType.Mob)
			    .OrderBy(spawner => UnityEngine.Random.value).ToList();
			int spawned = 0;
			foreach (EnemySpawner spawner in spawners)
			{
			    if (spawned >= initialMobSpawnCount) break;
			    while (spawner.TrySpawnIgnoringMaxCount())
			    {
			        spawned++;
			        if (spawned == initialMobSpawnCount) return;
			    }
			}
		}

		public void ResetEvent(string key)
		{
			// Original APK ARM64 ELF VA 0x14f2684.
			Get(key)?.Reset();
			ResetEventSpawners();
			forecastService.ClearSnapshotData();
		}

		public void ClearLastPlayedSlot()
		{
			// Original APK ARM64 ELF VA 0x14f2968.
			slotManager.ClearLastPlayedSlot();
		}

		private void ResetEventSpawners()
		{
			// Original APK ARM64 ELF VA 0x14f26b8.
			foreach (EnemySpawner spawner in enemiesModel.EventSpawners) spawner.ResetEventSpawnData();
		}

		private void ResetEventSpawnersForKey(string eventKey)
		{
			// Original APK ARM64 ELF VA 0x14f297c.
			foreach (EnemySpawner spawner in enemiesModel.EventSpawners)
			    if (spawner.Config.EventDef != null && spawner.Config.EventDef.Key == eventKey) spawner.ResetEventSpawnData();
		}

		public void ClaimEvent(string key)
		{
			// Original APK ARM64 ELF VA 0x14f2c70.
			var model = Get(key);
			if (model == null || model.State != EventState.Completed) return;
			model.Claim();
			ResetEvent(key);
		}
	}
}
