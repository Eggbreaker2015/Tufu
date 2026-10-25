using Codebase.Enemy;
using Multicast.UserData;

namespace Codebase.Events.Models
{
	public class EventsUd : UdObject
	{
		public UdValue<EventDifficulty> Difficulty { get; }

		public UdValue<int> State { get; }

		public UdValue<int> Progress { get; }

		public UdValue<float> ExtraDuration { get; }

		public UdValue<int> ActivatedAtSeconds { get; }

		public UdValue<WeaponType> WeaponType { get; }

		public UdValue<WeaponType> ResistType { get; }

		public UdValue<string> SnapshotData { get; }

		public UdValue<int> RosterMilestoneClaimedCount { get; }

		public UdValue<int> RosterLeagueIndex { get; }

		public EventSlotLockUd LastPlayedSlot { get; }

		public UdDictValue<double> Currencies { get; }

		public UdDictValue<double> CollectedCurrencies { get; }

		public UdDictValue<int> BoosterPurchases { get; }

		public EventsUd(UdArgs args) : base(args)
{
    Difficulty = new global::Multicast.UserData.UdValue<global::Codebase.Events.EventDifficulty>(Child("difficulty", true), (global::Codebase.Events.EventDifficulty)(-1));
    State = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("state", true));
    Progress = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("progress", true));
    ExtraDuration = new global::Multicast.UserData.UdValue<global::System.Single>(Child("extra_duration", true));
    ActivatedAtSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("activated_at_seconds", true));
    WeaponType = new global::Multicast.UserData.UdValue<global::Codebase.Enemy.WeaponType>(Child("weapon_type", true));
    ResistType = new global::Multicast.UserData.UdValue<global::Codebase.Enemy.WeaponType>(Child("resist_type", true));
    SnapshotData = new global::Multicast.UserData.UdValue<global::System.String>(Child("snapshot_data", true));
    RosterMilestoneClaimedCount = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("roster_milestone_claimed_count", true));
    RosterLeagueIndex = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("roster_league_index", true));
    LastPlayedSlot = new global::Codebase.Events.Models.EventSlotLockUd(Child("last_played_slot", true));
    Currencies = new global::Multicast.UserData.UdDictValue<global::System.Double>(Child("currencies", true));
    CollectedCurrencies = new global::Multicast.UserData.UdDictValue<global::System.Double>(Child("collected_currencies", true));
    BoosterPurchases = new global::Multicast.UserData.UdDictValue<global::System.Int32>(Child("booster_purchases", true));
}
	}
}
