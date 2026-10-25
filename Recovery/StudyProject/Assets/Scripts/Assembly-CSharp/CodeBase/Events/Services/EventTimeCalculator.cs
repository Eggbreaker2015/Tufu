using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Events.Models;
using Codebase.Player.Models;
using Codebase.Services.Time;

namespace Codebase.Events.Services
{
	public class EventTimeCalculator
	{
		[Inject]
		private ITimeService timeService;

		[Inject]
		private PlayerModel playerModel;

		private IReadOnlyList<EventModel> events;

		private Func<string, DateTime, bool> isSlotLockedFunc;

		public void Initialize(IReadOnlyList<EventModel> events, Func<string, DateTime, bool> isSlotLockedFunc)
		{
		}

		public TimeSpan GetTimeUntilEventEnds(EventModel eventModel)
		{
			return default;
		}

		public TimeSpan GetRemainingExtraTime(TimeSpan currentTime, TimeSpan endTime, TimeSpan extraDuration)
		{
			return default;
		}

		public (EventModel, DateTime?) FindNextEvent(Func<EventModel, bool> isUnlockedFunc)
		{
			return default;
		}

		public DateTime? GetNextStartTime(string eventKey, DateTime todayUtc, DateTime nowUtc, EventTimeInterval interval, int intervalDays)
		{
			return null;
		}

		public TimeSpan GetTimeUntilNextEvent(Func<EventModel, bool> isUnlockedFunc)
		{
			return default;
		}

		public static bool IsValidDay(DateTime date, int intervalDays)
		{
			return false;
		}
	}
}
