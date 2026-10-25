using Multicast.UserData;

namespace Codebase.Events.Models
{
	public class EventSlotLockUd : UdObject
	{
		public UdValue<string> EventKey { get; }

		public UdValue<int> SlotStartUnixSeconds { get; }

		public bool IsEmpty => false;

		public EventSlotLockUd(UdArgs args) : base(args)
{
    EventKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("event_key", true));
    SlotStartUnixSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("slot_start_unix_seconds", true));
}

		public void Clear()
		{
		}

		public void Set(string eventKey, int slotStartUnixSeconds)
		{
		}

		public bool Matches(string eventKey, int slotStartUnixSeconds)
		{
			return false;
		}
	}
}
