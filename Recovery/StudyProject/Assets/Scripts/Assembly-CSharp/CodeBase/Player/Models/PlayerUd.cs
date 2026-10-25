using Codebase.Core.UserData;
using Codebase.Unit;
using Multicast.UserData;

namespace Codebase.Player.Models
{
	public class PlayerUd : UdObject
	{
		public UdValue<string> HubGuestId { get; }

		public UdValue<int> CurrentLocationNumber { get; }

		public UdValue<int> UnlockedLocationNumber { get; }

		public UdValue<int> BossProgressLocationNumber { get; }

		public UdValue<int> PortalGemsCollected { get; }

		public UdValue<double> Gems { get; }

		public UdValue<bool> VisitedLastUnlockedLocation { get; }

		public UdValue<bool> SeenEventTutorial { get; }

		public UdValue<bool> SeenBossTutorial { get; }

		public UdValue<bool> SeenCrystalsTutorial { get; }

		public UdValue<bool> SeenAmuletCrystalsEnchantment { get; }

		public UdValue<bool> SeenAmuletGraphTutorial { get; }

		public UdValue<bool> PlayerInBossArena { get; }

		public UdValue<bool> IsPortalUnlockAnimationShown { get; }

		public UdValue<int> DeathsCount { get; }

		public UdListObject<ItemDataUd> Items { get; }

		public UdListObject<ItemSlotDataUd> ItemSlots { get; }

		public UdListObject<StatDataUd> Stats { get; }

		public UdListValue<string> Loadout { get; }

		public UdValue<Vector3Ud> LastPosition { get; }

		public UdValue<bool> ItemsReworkMigrated { get; }

		public UdValue<long> UnstackCooldownTimestamp { get; }

		public PlayerUd(UdArgs args)
			: base(args)
		{
			// Native ELF VA 0x1461bc0. Exact Child keys and cache flags come from RELA-resolved literals.
			HubGuestId = new UdValue<string>(Child("gid", true));
			CurrentLocationNumber = new UdValue<int>(Child("current_location_number", true), 1);
			UnlockedLocationNumber = new UdValue<int>(Child("unlocked_location_number", true), 1);
			BossProgressLocationNumber = new UdValue<int>(Child("boss_progress_location_number", true), 1);
			PortalGemsCollected = new UdValue<int>(Child("portal_gems_collected", true));
			Gems = new UdValue<double>(Child("gems", true));
			VisitedLastUnlockedLocation = new UdValue<bool>(Child("visited_last_unlocked_location", true), true);
			SeenEventTutorial = new UdValue<bool>(Child("seen_event_tutorial", true));
			SeenBossTutorial = new UdValue<bool>(Child("seen_boss_tutorial", true));
			SeenCrystalsTutorial = new UdValue<bool>(Child("seen_crystals_tutorial", true));
			SeenAmuletCrystalsEnchantment = new UdValue<bool>(Child("seen_amulet_crystals_enchantment", true));
			SeenAmuletGraphTutorial = new UdValue<bool>(Child("seen_amulet_graph_tutorial", true));
			DeathsCount = new UdValue<int>(Child("deaths_count", true));
			PlayerInBossArena = new UdValue<bool>(Child("player_in_dungeon", true));
			IsPortalUnlockAnimationShown = new UdValue<bool>(Child("portal_unlock_animation_shown", true));
			Items = new UdListObject<ItemDataUd>(Child("items", true), child => new ItemDataUd(child));
			ItemSlots = new UdListObject<ItemSlotDataUd>(Child("item_slots", true), child => new ItemSlotDataUd(child));
			Stats = new UdListObject<StatDataUd>(Child("stats", false), child => new StatDataUd(child));
			Loadout = new UdListValue<string>(Child("loadout", true));
			LastPosition = new UdValue<Vector3Ud>(Child("last_position", true));
			ItemsReworkMigrated = new UdValue<bool>(Child("items_rework_migrated", true));
			UnstackCooldownTimestamp = new UdValue<long>(Child("unstack_cooldown_ts", true));
		}
	}
}
