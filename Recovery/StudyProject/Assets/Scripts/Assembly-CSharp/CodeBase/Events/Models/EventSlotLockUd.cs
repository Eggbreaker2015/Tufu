using Multicast.UserData;

namespace Codebase.Events.Models
{
	public class EventSlotLockUd : UdObject
	{
		public UdValue<string> EventKey { get; }

		public UdValue<int> SlotStartUnixSeconds { get; }

		public bool IsEmpty => string.IsNullOrEmpty(EventKey.Value);

		public EventSlotLockUd(UdArgs args) : base(args)
{
    EventKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("event_key", true));
    SlotStartUnixSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("slot_start_unix_seconds", true));
}

		public void Clear()
		{
			// Original APK ARM64 ELF VA 0x14e9048.
			EventKey.Value = string.Empty;
			SlotStartUnixSeconds.Value = 0;
		}

		public void Set(string eventKey, int slotStartUnixSeconds)
		{
			// Original APK ARM64 ELF VA 0x14e8f9c.
			EventKey.Value = eventKey;
			SlotStartUnixSeconds.Value = slotStartUnixSeconds;
		}

		public bool Matches(string eventKey, int slotStartUnixSeconds)
		{
			// Original APK ARM64 ELF VA 0x14e8ae0.
			return EventKey.Value == eventKey && SlotStartUnixSeconds.Value == slotStartUnixSeconds;
		}
	}
}
