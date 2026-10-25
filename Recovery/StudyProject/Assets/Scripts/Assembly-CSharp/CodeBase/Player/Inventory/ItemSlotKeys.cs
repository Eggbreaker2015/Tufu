using System.Linq;
using System.Collections.Generic;
using Codebase.Player.Models;

namespace Codebase.Player.Inventory
{
	public static class ItemSlotKeys
	{
		public const string Weapon_1 = "weapon_1";

		public const string Weapon_2 = "weapon_2";

		public const string Weapon_3 = "weapon_3";

		public const string Weapon_4 = "weapon_4";

		public const string Weapon_5_Vip = "weapon_5_vip";

		public const string Helmet_1 = "helmet_1";

		public const string Shield_1 = "shield_1";

		public static readonly HashSet<string> ChainWeaponSlotKeys = new HashSet<string> { "weapon_5_vip", "weapon_3", "weapon_2", "weapon_4" };

		public static bool HasChainWeaponEquipped(PlayerModel player)
		{
			// Native ELF VA 0x1465964, predicate 0x1465d48.
			return player.ItemSlots.Any(slot => slot.IsEquipped && slot.SlotType == ItemSlotType.Weapon && ChainWeaponSlotKeys.Contains(slot.Key));
		}

		public static int EquippedWeaponCount(PlayerModel player)
		{
			// Native ELF VA 0x1465a78, predicate 0x1465df0.
			return player.ItemSlots.Count(slot => slot.IsEquipped && slot.SlotType == ItemSlotType.Weapon);
		}
	}
}
