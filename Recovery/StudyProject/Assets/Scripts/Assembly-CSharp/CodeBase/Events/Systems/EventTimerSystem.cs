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
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void ResumeStuckCompletedEvents()
		{
		}

		private void CheckAndCompleteExpiredEvents()
		{
		}

		private bool IsOnlineNow(DateTime nowUtc)
		{
			return false;
		}

		private bool ExpiredDuringCurrentSession(EventModel eventModel)
		{
			return false;
		}

		private bool TryOpenTimeIsOverView(EventModel eventModel, DateTime nowUtc)
		{
			return false;
		}

		private bool HasExtraTimeBoosters(EventModel eventModel, DateTime nowUtc)
		{
			return false;
		}

		private void OnTimeIsOverViewClosed(string eventKey)
		{
		}

		private void UpdateTimeIsOverView()
		{
		}

		private TimeSpan GetTimeUntilEventEnds(EventModel eventModel, DateTime nowUtc)
		{
			return default;
		}

		private void UpdatePendingEventEndView()
		{
		}

		private bool TryOpenLeagueNotification(EventModel model)
		{
			return false;
		}

		private void OnLeagueNotificationClosed()
		{
		}

		private void ResetCompletedEvent(EventModel eventModel)
		{
		}

		private void CompleteEvent(EventModel eventModel)
		{
		}

		private void ExpireReadyEvent(EventModel eventModel)
		{
		}

		private void CheckAndPrepareEvents()
		{
		}

		private void RefreshEventSpawners()
		{
		}

		private bool IsEventTimeActive(EventModel eventModel, DateTime nowUtc)
		{
			return false;
		}

		private bool IsEventTimeActiveWithExtra(EventModel eventModel, DateTime nowUtc)
		{
			return false;
		}

		private bool IsInExtraTime(TimeSpan currentTime, TimeSpan endTime, TimeSpan extraTime)
		{
			return false;
		}

		private bool IsValidDay(DateTime date, int intervalDays)
		{
			return false;
		}

		private void PrepareEvent(EventModel eventModel, DateTime nowUtc)
		{
		}
	}
}
