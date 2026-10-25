using System;
using Codebase.Events.Models;

namespace Codebase.Events.Services
{
	public class EventSlotManager
	{
		private EventSlotLockUd LastPlayedSlot => null;

		public bool IsSlotLocked(string eventKey, DateTime slotStartUtc)
		{
			return false;
		}

		public bool IsCurrentEventSlotLocked(EventModel eventModel, DateTime nowUtc)
		{
			return false;
		}

		public void LockCurrentEventSlot(EventModel eventModel, DateTime nowUtc)
		{
		}

		public void ClearLastPlayedSlot()
		{
		}

		public bool TryGetCurrentSlotStartUnixSeconds(EventModel eventModel, DateTime nowUtc, out int slotStartUnixSeconds)
		{
			slotStartUnixSeconds = default;
			return false;
		}
	}
}
