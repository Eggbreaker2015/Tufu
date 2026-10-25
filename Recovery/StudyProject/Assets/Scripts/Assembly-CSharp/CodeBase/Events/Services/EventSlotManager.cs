using System;
using Codebase.Core.UserData;
using Codebase.Events.Models;

namespace Codebase.Events.Services
{
	public class EventSlotManager
	{
		private EventSlotLockUd LastPlayedSlot => UserDataProvider.Resolve<EventsUd>().LastPlayedSlot;

		public bool IsSlotLocked(string eventKey, DateTime slotStartUtc)
		{
			// Original APK ARM64 ELF VA 0x14e8988.
			if (LastPlayedSlot.IsEmpty) return false;
			int seconds = (int)new DateTimeOffset(slotStartUtc, TimeSpan.Zero).ToUnixTimeSeconds();
			return LastPlayedSlot.Matches(eventKey, seconds);
		}

		public bool IsCurrentEventSlotLocked(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14e8ba0.
			if (eventModel == null || !TryGetCurrentSlotStartUnixSeconds(eventModel, nowUtc, out int seconds)) return false;
			return !LastPlayedSlot.IsEmpty && LastPlayedSlot.Matches(eventModel.Key, seconds);
		}

		public void LockCurrentEventSlot(EventModel eventModel, DateTime nowUtc)
		{
			// Original APK ARM64 ELF VA 0x14e8f48.
			if (eventModel != null && TryGetCurrentSlotStartUnixSeconds(eventModel, nowUtc, out int seconds))
			    LastPlayedSlot.Set(eventModel.Key, seconds);
		}

		public void ClearLastPlayedSlot()
		{
			// Original APK ARM64 ELF VA 0x14e9030.
			LastPlayedSlot.Clear();
		}

		public bool TryGetCurrentSlotStartUnixSeconds(EventModel eventModel, DateTime nowUtc, out int slotStartUnixSeconds)
		{
			// Original APK ARM64 ELF VA 0x14e8c08.
			slotStartUnixSeconds = 0;
			DateTime today = nowUtc.Date;
			if (!EventTimeCalculator.IsValidDay(today, eventModel.Def.IntervalDays)) return false;
			foreach (EventTimeInterval interval in eventModel.Def.StartIntervals)
			{
			    if (!interval.IsActiveAt(nowUtc.TimeOfDay)) continue;
			    slotStartUnixSeconds = (int)new DateTimeOffset(today + interval.StartTime, TimeSpan.Zero).ToUnixTimeSeconds();
			    return true;
			}
			return false;
		}
	}
}
