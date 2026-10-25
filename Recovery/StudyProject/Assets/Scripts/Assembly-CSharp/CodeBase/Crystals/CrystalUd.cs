using Multicast.UserData;

namespace Codebase.Crystals
{
	public class CrystalUd : UdObject
	{
		public UdValue<bool> IsUnlocked { get; }

		public UdValue<bool> FirstGuaranteedEnchantUsed { get; }

		public UdListObject<CrystalItemUd> Crystals { get; }

		public UdListObject<CrystalSlotDataUd> Slots { get; }

		public UdValue<long> LastGeodeTimestamp { get; }

		public UdValue<long> LastStonesResetDay { get; }

		public UdDictValue<int> UpgradeLevels { get; }

		public UdValue<int> NextCrystalId { get; }

		public UdValue<int> GuaranteeEnchantProgress { get; }

		public UdValue<int> GeodesOpenedCount { get; }

		public UdValue<int> ObjectiveGeodesProgress { get; }

		public UdValue<int> ObjectiveKillCount { get; }

		public UdValue<bool> IsObjectiveWaitingForRespawn { get; }

		public CrystalUd(UdArgs args) : base(args)
{
    IsUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_unlocked", true));
    Crystals = new global::Multicast.UserData.UdListObject<global::Codebase.Crystals.CrystalItemUd>(Child("crystals", true), childArgs => new global::Codebase.Crystals.CrystalItemUd(childArgs));
    Slots = new global::Multicast.UserData.UdListObject<global::Codebase.Crystals.CrystalSlotDataUd>(Child("slots", true), childArgs => new global::Codebase.Crystals.CrystalSlotDataUd(childArgs));
    LastGeodeTimestamp = new global::Multicast.UserData.UdValue<global::System.Int64>(Child("last_geode_timestamp", true));
    LastStonesResetDay = new global::Multicast.UserData.UdValue<global::System.Int64>(Child("last_stones_reset_day", true));
    UpgradeLevels = new global::Multicast.UserData.UdDictValue<global::System.Int32>(Child("upgrade_levels", true));
    NextCrystalId = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("next_crystal_id", true), 1);
    GuaranteeEnchantProgress = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("guarantee_enchant_progress", true));
    GeodesOpenedCount = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("geodes_opened_count", true));
    ObjectiveGeodesProgress = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("objective_geodes_progress", true));
    ObjectiveKillCount = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("objective_kill_count", true));
    IsObjectiveWaitingForRespawn = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_objective_waiting_for_respawn", true));
    FirstGuaranteedEnchantUsed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_first_guaranteed_enchant_used", true));
}
	}
}
