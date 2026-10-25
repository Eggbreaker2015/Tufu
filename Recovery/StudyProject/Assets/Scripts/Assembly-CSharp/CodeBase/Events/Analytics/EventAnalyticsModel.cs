using System;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Events.Models;
using Codebase.Services.Time;
using UniMob;

namespace Codebase.Events.Analytics
{
	public class EventAnalyticsModel : Model<EventAnalyticsUd>
	{
		[Inject]
		private IAnalyticsWrapper analytics;

		[Inject]
		private ITimeService timeService;

		public int LastOnlineUtcSeconds => 0;

		public EventAnalyticsModel(Lifetime lifetime)
			: base(default(Lifetime))
		{
		}

		public void MarkScheduleCheckUtc(int nowUtcSeconds)
		{
		}

		public void HeartbeatUtc(int nowUtcSeconds)
		{
		}

		public void MarkReady(EventModel eventModel, DateTime nowUtc)
		{
		}

		public void ReportReadyExpiredAsSkippedOnline(EventModel eventModel)
		{
		}

		public void ReportOfflineSkipped(EventsRepository eventsRepository)
		{
		}

		private void ReportOfflineSkippedForEvent(EventsRepository eventsRepository, EventModel eventModel, DateTime lastCheckUtc, DateTime nowUtc)
		{
		}

		public void OnEventStarted(EventModel eventModel)
		{
		}

		public void IncrementEventMobKilled(string eventKey)
		{
		}

		public void IncrementAdsWatched(string eventKey)
		{
		}

		public void AddPlaytimeMinutes(string eventKey, int minutes)
		{
		}

		public void ReportCompleted(EventModel completedEvent)
		{
		}

		private bool TryGetActiveScheduleStamp(EventModel eventModel, DateTime nowUtc, out string stamp)
		{
			stamp = null;
			return false;
		}

		private static bool IsValidDay(DateTime dateUtc, int intervalDays)
		{
			return false;
		}

		private static (DateTime, DateTime) CreateWindow(DateTime endDayUtc, EventTimeInterval interval)
		{
			return default;
		}

		private static string FormatScheduleStamp(DateTime endDayUtc, EventTimeInterval interval)
		{
			return null;
		}

		private int NowUtcSeconds()
		{
			return 0;
		}
	}
}
