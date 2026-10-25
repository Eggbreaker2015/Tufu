using Codebase.Core.Flows;
using Codebase.Player.Inventory;
using Codebase.Core;
using Codebase.Player.Models;

namespace Codebase.UI.Flows
{
	public struct ItemSlotUnlockFlow : IFlow
	{
		public ItemSlotType SlotType;
		public int Index;
		public bool IsVip;
		public static ItemSlotUnlockFlow? For(string slotKey, bool onlyIfLocked = true)
		{
			if (string.IsNullOrEmpty(slotKey))
				return null;
			var slot = GameContext.Get<PlayerModel>().ItemSlots.Find(itemSlot => itemSlot.Key == slotKey);
			if (slot == null || slot.IsVip || (onlyIfLocked && slot.IsUnlocked))
				return null;
			return new ItemSlotUnlockFlow
			{
				SlotType = slot.SlotType,
				Index = ParseIndex(slot.Key),
				IsVip = false
			};
		}

		private static int ParseIndex(string key)
		{
			foreach (string part in key.Split('_'))
				if (int.TryParse(part, out int index))
					return index - 1;
			return 0;
		}
	}
}
