using System;
using Codebase.Player.Inventory;

namespace Codebase.Core
{
	[Serializable]
	public class ItemSlotWrapper
	{
		public string Key;

		public string SlotKey;

		public ItemSlotType SlotType;

		public bool IsUnlocked;

		public bool IsPinned;

		public ItemSlotWrapper(ItemSlotModel item)
		{
		}

		public ItemSlotWrapper()
		{
		}
	}
}
