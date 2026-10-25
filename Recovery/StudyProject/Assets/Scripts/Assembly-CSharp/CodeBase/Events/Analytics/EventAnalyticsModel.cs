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

		// Original ELF VA0x14fc824; persisted online timestamp also gates the extension popup.
		public int LastOnlineUtcSeconds => Data.LastOnlineUtcSeconds.Value;

		public EventAnalyticsModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public void MarkScheduleCheckUtc(int nowUtcSeconds)
        {
            // Original ELF VA0x14fc894.
            Data.LastScheduleCheckUtcSeconds.Value = nowUtcSeconds;
        }

		public void HeartbeatUtc(int nowUtcSeconds)
        {
            // Original ELF VA0x14fc910: both stamps receive the same caller-provided seconds.
            Data.LastOnlineUtcSeconds.Value = nowUtcSeconds;
            Data.LastScheduleCheckUtcSeconds.Value = nowUtcSeconds;
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
            // Original ELF VA0x14fd344; unused analytics methods remain outside this minimal restoration.
            return unchecked((int)new DateTimeOffset(timeService.Now.ToDateTime().ToUniversalTime()).ToUnixTimeSeconds());
        }
	}
}
