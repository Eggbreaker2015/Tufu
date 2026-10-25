using System.Linq;
using Codebase.UI.Events;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using System;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Services.Time;
using Scellecs.Morpeh;

namespace Codebase.Events.Systems
{
	public class EventTimerSystem : SystemBase
	{
		private const float updateInterval = 1f;

		private const int onlineThresholdSeconds = 90;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		private float timeSinceLastUpdate;

		private PopupView timeIsOverView;

		private string timeIsOverEventKey;

		private string pendingEventEndViewKey;

		private bool hasResumedStuckCompletedEvents;

		private DateTime sessionStartUtc;

		private PopupView leagueNotificationView;

		private string leagueNotificationShownForEventKey;

		public override void OnAwake()
		{
			// Original APK ARM64 ELF VA 0x14dcfa4.
			timeSinceLastUpdate = 1f;
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original APK ARM64 ELF VA 0x14dcfb0.
			if (eventsRepository == null || timeService == null) return;
			if (!hasResumedStuckCompletedEvents)
			{
			    hasResumedStuckCompletedEvents = true;
			    sessionStartUtc = timeService.Now.ToDateTime().ToUniversalTime();
			    ResumeStuckCompletedEvents();
			}
			timeSinceLastUpdate += deltaTime;
			if (timeSinceLastUpdate < updateInterval) return;
			timeSinceLastUpdate = 0f;
			CheckAndCompleteExpiredEvents();
			UpdateTimeIsOverView();
			UpdatePendingEventEndView();
			CheckAndPrepareEvents();
			RefreshEventSpawners();
		}

		private void ResumeStuckCompletedEvents()
		{
			// Original APK ARM64 ELF VA 0x14dd108.
			foreach (EventModel model in eventsRepository.Events)
			    if (model.State == EventState.Completed)
			    {
			        pendingEventEndViewKey = model.Key;
			        UpdatePendingEventEndView();
			    }
		}

		private void CheckAndCompleteExpiredEvents()
		{
			// Original APK ARM64 ELF VA 0x14dd3f4.
			DateTime now = timeService.Now.ToDateTime().ToUniversalTime();
			bool online = IsOnlineNow(now);
			foreach (EventModel model in eventsRepository.Events)
			{
			    if (model.State == EventState.NotStarted && leagueNotificationShownForEventKey == model.Key)
			        leagueNotificationShownForEventKey = null;
			    if (model.IsActive)
			    {
			        if (IsEventTimeActiveWithExtra(model, now)) continue;
			        if (online && ExpiredDuringCurrentSession(model) && TryOpenTimeIsOverView(model, now)) continue;
			        pendingEventEndViewKey = model.Key;
			        CompleteEvent(model);
			        UpdatePendingEventEndView();
			    }
			    else if (model.IsReady)
			    {
			        if (!model.ActivatedAt.HasValue && !IsEventTimeActive(model, now)) ExpireReadyEvent(model);
			    }
			    else if (model.IsClaimed) ResetCompletedEvent(model);
			}
		}

		private bool IsOnlineNow(DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14de00c.
			int lastOnline = eventAnalyticsModel?.LastOnlineUtcSeconds ?? 0;
			return lastOnline > 0 && (int)new DateTimeOffset(nowUtc, TimeSpan.Zero).ToUnixTimeSeconds() - lastOnline <= onlineThresholdSeconds;
		}

		private bool ExpiredDuringCurrentSession(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14de4cc.
			return eventModel.ActivatedAt.HasValue &&
			    eventModel.ActivatedAt.Value + TimeSpan.FromMinutes(eventModel.DurationMinutes) + TimeSpan.FromMinutes(eventModel.ExtraDuration) >= sessionStartUtc;
		}

		private bool TryOpenTimeIsOverView(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14de66c.
			if (!HasExtraTimeBoosters(eventModel, nowUtc)) return false;
			if (timeIsOverView != null && timeIsOverEventKey == eventModel.Key) return true;
			timeIsOverEventKey = eventModel.Key;
			string key = eventModel.Def?.EventTimeIsOverViewKey;
			timeIsOverView = navigator.Open<PopupView>(string.IsNullOrEmpty(key) ? "Assets/Prefabs/UI/Event/EventTimeIsOver.prefab" : key) as RosterEventTimeIsOverView;
			if (timeIsOverView == null)
			{
			    timeIsOverEventKey = null;
			    return false;
			}
			timeIsOverView.SetUserCloseCallback(() => OnTimeIsOverViewClosed(eventModel.Key));
			return true;
		}

		private bool HasExtraTimeBoosters(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14dedb0.
			if (!eventModel.ActivatedAt.HasValue) return false;
			float additionalMinutes = 0f;
			foreach (EventBoosterModel booster in eventModel.Boosters)
			{
			    if (!booster.HasRemainingPurchases) continue;
			    int minutes = booster.Def.Rewards.OfType<EventReward>().Sum(reward => reward.ExtraDurationMinutes);
			    if (minutes <= 0) continue;
			    if (booster.Def.IsUnlimited) return true;
			    additionalMinutes += booster.RemainingPurchases * minutes;
			}
			return additionalMinutes > 0f && nowUtc < eventModel.ActivatedAt.Value + TimeSpan.FromMinutes(eventModel.DurationMinutes) +
			    TimeSpan.FromMinutes(eventModel.ExtraDuration) + TimeSpan.FromMinutes(additionalMinutes);
		}

		private void OnTimeIsOverViewClosed(string eventKey)
		{
			// Original APK ARM64 ELF VA 0x14df398.
			timeIsOverView = null;
			timeIsOverEventKey = null;
			EventModel model = eventsRepository.Get(eventKey);
			if (model == null || !model.IsActive) return;
			if (!IsEventTimeActiveWithExtra(model, timeService.Now.ToDateTime().ToUniversalTime()))
			{
			    pendingEventEndViewKey = eventKey;
			    CompleteEvent(model);
			    UpdatePendingEventEndView();
			}
		}

		private void UpdateTimeIsOverView()
		{
			// Original APK ARM64 ELF VA 0x14dd924.
			if (timeIsOverView == null || string.IsNullOrEmpty(timeIsOverEventKey)) return;
			var model = eventsRepository.Get(timeIsOverEventKey);
			if (model == null || !model.IsActive || GetTimeUntilEventEnds(model, timeService.Now.ToDateTime().ToUniversalTime()) > TimeSpan.Zero)
			    timeIsOverView.Close();
		}

		private TimeSpan GetTimeUntilEventEnds(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14df504.
			if (eventModel == null || !eventModel.ActivatedAt.HasValue) return TimeSpan.Zero;
			TimeSpan remaining = eventModel.ActivatedAt.Value + TimeSpan.FromMinutes(eventModel.DurationMinutes) +
			    TimeSpan.FromMinutes(eventModel.ExtraDuration) - nowUtc;
			return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
		}

		private void UpdatePendingEventEndView()
		{
			// Original APK ARM64 ELF VA 0x14ddb04.
			if (string.IsNullOrEmpty(pendingEventEndViewKey)) return;
			var model = eventsRepository.Get(pendingEventEndViewKey);
			if (model != null)
			{
			    if (model.State == EventState.Completed)
			    {
			        if (TryOpenLeagueNotification(model)) return;
			        navigator.Open(model.Def?.EventEndViewKey ?? "Assets/Prefabs/UI/Event/EventEnds.prefab");
			    }
			    else if (model.State == EventState.Active) return;
			}
			pendingEventEndViewKey = null;
		}

		private bool TryOpenLeagueNotification(EventModel model)
		{
			// Original APK ARM64 ELF VA 0x14df6ec.
			if (model.Key != "crystal_event_roster") return false;
			if (leagueNotificationView != null) return true;
			if (leagueNotificationShownForEventKey == model.Key) return false;
			var plan = eventsRepository.EventPlan;
			if (!plan.HasValue || model.RosterLeagueIndex <= plan.Value.LeagueIndex) return false;
			leagueNotificationShownForEventKey = model.Key;
			leagueNotificationView = navigator.Open<PopupView>("Assets/Prefabs/UI/Event/Roster/RosterEventEndLeagueNotificationView.prefab");
			leagueNotificationView.SetUserCloseCallback(OnLeagueNotificationClosed);
			return true;
		}

		private void OnLeagueNotificationClosed()
		{
			// Original APK ARM64 ELF VA 0x14df974.
			leagueNotificationView = null;
			UpdatePendingEventEndView();
		}

		private void ResetCompletedEvent(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14ded54.
			leagueNotificationShownForEventKey = null;
			eventsRepository.ResetEvent(eventModel.Key);
		}

		private void CompleteEvent(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14de90c.
			bool incomplete = eventModel.State == EventState.Active && !eventModel.IsProgressComplete;
			eventModel.Complete();
			if (eventModel.State != EventState.Completed) return;
			// Learning adaptation: mail is excluded. Earned unclaimed roster milestones remain
			// in the original event data until the end-view Claim button delivers them locally.
			if (incomplete && eventModel.DifficultyDef?.FailedRewards != null)
			    App.ExecuteCommand(new BakeRewardsCommand { Rewards = eventModel.DifficultyDef.FailedRewards });
		}

		private void ExpireReadyEvent(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14decfc.
			eventAnalyticsModel?.ReportReadyExpiredAsSkippedOnline(eventModel);
			eventsRepository.ResetEvent(eventModel.Key);
		}

		private void CheckAndPrepareEvents()
		{
			// Original APK ARM64 ELF VA 0x14ddbf4.
			DateTime now = timeService.Now.ToDateTime().ToUniversalTime();
			foreach (EventModel model in eventsRepository.Events)
			    if (model.State == EventState.NotStarted && eventsRepository.IsEventUnlocked(model) &&
			        IsEventTimeActive(model, now) && !eventsRepository.IsCurrentEventSlotLocked(model, now)) PrepareEvent(model, now);
		}

		private void RefreshEventSpawners()
		{
			// Original APK ARM64 ELF VA 0x14ddfdc.
			eventsRepository.CurrentEvent?.RefreshSpawners();
		}

		private bool IsEventTimeActive(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14dea90.
			return eventModel.Def.StartIntervals.Any(interval => interval.IsActiveAt(nowUtc.TimeOfDay) && IsValidDay(nowUtc.Date, eventModel.Def.IntervalDays));
		}

		private bool IsEventTimeActiveWithExtra(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14de0f4.
			if (eventModel.ActivatedAt.HasValue)
			    return nowUtc < eventModel.ActivatedAt.Value + TimeSpan.FromMinutes(eventModel.DurationMinutes) + TimeSpan.FromMinutes(eventModel.ExtraDuration);
			TimeSpan current = nowUtc.TimeOfDay, extra = TimeSpan.FromMinutes(eventModel.ExtraDuration);
			return eventModel.Def.StartIntervals.Any(interval => interval.IsActiveAt(current) || IsInExtraTime(current, interval.EndTime, extra));
		}

		private bool IsInExtraTime(TimeSpan currentTime, TimeSpan endTime, TimeSpan extraTime)
		{
			// Original APK ARM64 ELF VA 0x14dfa90.
			if (extraTime <= TimeSpan.Zero) return false;
			TimeSpan extendedEnd = endTime + extraTime;
			if (extendedEnd <= TimeSpan.FromHours(24)) return currentTime >= endTime && currentTime < extendedEnd;
			extendedEnd -= TimeSpan.FromHours(24);
			return currentTime >= endTime || currentTime < extendedEnd;
		}

		private bool IsValidDay(DateTime date, int intervalDays)
		{
			// Original APK ARM64 ELF VA 0x14dfa10.
			return intervalDays == 0 || date.DayOfYear % (intervalDays + 1) == 0;
		}

		private void PrepareEvent(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14df994.
			if (eventsRepository.MakeEventAvailable(eventModel.Key)) eventAnalyticsModel?.MarkReady(eventModel, nowUtc);
		}
	}
}
