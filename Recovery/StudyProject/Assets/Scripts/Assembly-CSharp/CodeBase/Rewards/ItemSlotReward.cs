using System;
using Codebase.Player.Inventory;
using Codebase.Utilities;

namespace Codebase.Rewards
{
	[Serializable]
	public class ItemSlotReward : RewardBase
	{
		[KeysDropdown(typeof(ItemSlotKeys))]
		public string SlotKey;
	}
}
