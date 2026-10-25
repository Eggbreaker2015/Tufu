using Codebase.Player.Inventory;
using Multicast.UserData;

namespace Codebase.Player.Models
{
	public class ItemSlotDataUd : UdObject
	{
		public UdValue<string> ItemKey { get; }

		public UdValue<string> SlotKey { get; }

		public UdValue<ItemSlotType> SlotType { get; }

		public UdValue<bool> IsUnlocked { get; }

		public UdValue<bool> IsPinned { get; }

		public ItemSlotDataUd(UdArgs args)
			: base(args)
		{
			// Native ELF VA 0x1462d3c. Exact Child keys and cache flags come from RELA-resolved literals.
			ItemKey = new UdValue<string>(Child("item_key", true));
			SlotKey = new UdValue<string>(Child("slot_key", true));
			SlotType = new UdValue<ItemSlotType>(Child("slot_type", true), default(ItemSlotType));
			IsUnlocked = new UdValue<bool>(Child("is_unlocked", true));
			IsPinned = new UdValue<bool>(Child("is_pinned", true));
		}
	}
}
