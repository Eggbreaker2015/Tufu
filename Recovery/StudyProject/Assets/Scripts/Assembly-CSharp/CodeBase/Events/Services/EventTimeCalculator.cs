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
			// Original APK ARM64 ELF VA 0x14e9154.
			this.events = events;
			this.isSlotLockedFunc = isSlotLockedFunc;
		}

		public TimeSpan GetTimeUntilEventEnds(EventModel eventModel)
		{
			// Original APK ARM64 ELF VA 0x14e9184.
			TimeSpan extra = TimeSpan.FromMinutes(eventModel.ExtraDuration);
			DateTime now = timeService.Now.ToDateTime().ToUniversalTime();
			if (eventModel.IsActive && eventModel.ActivatedAt.HasValue)
			{
			    TimeSpan remaining = eventModel.ActivatedAt.Value + TimeSpan.FromMinutes(eventModel.DurationMinutes) + extra - now;
			    return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
			}
			TimeSpan current = now.TimeOfDay;
			TimeSpan day = TimeSpan.FromHours(24);
			foreach (EventTimeInterval interval in eventModel.Def.StartIntervals)
			{
			    if (interval.IsActiveAt(current))
			        return (interval.EndTime > current ? interval.EndTime - current : day - current + interval.EndTime) + extra;
			    TimeSpan remaining = GetRemainingExtraTime(current, interval.EndTime, extra);
			    if (remaining > TimeSpan.Zero) return remaining;
			}
			return TimeSpan.Zero;
		}

		public TimeSpan GetRemainingExtraTime(TimeSpan currentTime, TimeSpan endTime, TimeSpan extraDuration)
		{
			// Original APK ARM64 ELF VA 0x14e9c98.
			if (extraDuration <= TimeSpan.Zero) return TimeSpan.Zero;
			TimeSpan extendedEnd = endTime + extraDuration;
			TimeSpan day = TimeSpan.FromHours(24);
			if (extendedEnd <= day)
			    return currentTime >= endTime && currentTime < extendedEnd ? extendedEnd - currentTime : TimeSpan.Zero;
			extendedEnd -= day;
			if (currentTime >= endTime) return day - currentTime + extendedEnd;
			return currentTime < extendedEnd ? extendedEnd - currentTime : TimeSpan.Zero;
		}

		public (EventModel, DateTime?) FindNextEvent(Func<EventModel, bool> isUnlockedFunc)
		{
			// Original APK ARM64 ELF VA 0x14e9e98.
			DateTime now = timeService.Now.ToDateTime().ToUniversalTime();
			DateTime today = now.Date;
			EventModel selected = null;
			DateTime? earliest = null;
			foreach (EventModel model in events)
			{
			    if (!isUnlockedFunc(model)) continue;
			    foreach (EventTimeInterval interval in model.Def.StartIntervals)
			    {
			        DateTime? start = GetNextStartTime(model.Key, today, now, interval, model.Def.IntervalDays);
			        if (!start.HasValue || (earliest.HasValue && start.Value >= earliest.Value)) continue;
			        selected = model;
			        earliest = start;
			    }
			}
			return (selected, earliest);
		}

		public DateTime? GetNextStartTime(string eventKey, DateTime todayUtc, DateTime nowUtc, EventTimeInterval interval, int intervalDays)
		{
			// Original APK ARM64 ELF VA 0x14ea57c.
			DateTime start = todayUtc + interval.StartTime;
			bool valid = IsValidDay(todayUtc, intervalDays);
			if (interval.IsActiveAt(nowUtc.TimeOfDay) && valid && !isSlotLockedFunc(eventKey, start)) return start;
			if (nowUtc.TimeOfDay < interval.StartTime && valid && !isSlotLockedFunc(eventKey, start)) return start;
			for (int day = 1; day < 367; day++)
			{
			    DateTime date = todayUtc.AddDays(day);
			    if (!IsValidDay(date, intervalDays)) continue;
			    DateTime candidate = date + interval.StartTime;
			    if (!isSlotLockedFunc(eventKey, candidate)) return candidate;
			}
			return null;
		}

		public TimeSpan GetTimeUntilNextEvent(Func<EventModel, bool> isUnlockedFunc)
		{
			// Original APK ARM64 ELF VA 0x14ea814.
			DateTime? next = FindNextEvent(isUnlockedFunc).Item2;
			if (!next.HasValue) return TimeSpan.Zero;
			TimeSpan remaining = next.Value - timeService.Now.ToDateTime();
			return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
		}

		public static bool IsValidDay(DateTime date, int intervalDays)
		{
			// Original APK ARM64 ELF VA 0x14e90cc.
			return intervalDays == 0 || date.DayOfYear % (intervalDays + 1) == 0;
		}
	}
}
