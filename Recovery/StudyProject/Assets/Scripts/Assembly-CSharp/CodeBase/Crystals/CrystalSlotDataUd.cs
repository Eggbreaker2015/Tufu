using Multicast.UserData;

namespace Codebase.Crystals
{
	public class CrystalSlotDataUd : UdObject
	{
		public UdValue<string> SlotKey { get; }

		public UdValue<bool> IsUnlocked { get; }

		public CrystalSlotDataUd(UdArgs args) : base(args)
{
    SlotKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("slot_key", true));
    IsUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_unlocked", true));
}
	}
}
