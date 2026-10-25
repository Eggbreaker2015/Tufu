using Multicast.UserData;

namespace Codebase.Events.Analytics
{
	public class EventAnalyticsUd : UdObject
	{
		public UdValue<int> LastScheduleCheckUtcSeconds { get; }

		public UdValue<int> LastOnlineUtcSeconds { get; }

		public UdValue<int> EventMobsKilled { get; }

		public UdValue<int> AdsWatched { get; }

		public UdValue<int> PlaytimeMinutes { get; }

		public UdValue<string> ActiveEventKey { get; }

		public UdValue<string> ActiveScheduleStamp { get; }

		public UdValue<string> LastCompletedId { get; }

		public UdDictValue<string> ReadyStampByEventKey { get; }

		public UdDictValue<string> LastSkippedStampByEventKey { get; }

		public EventAnalyticsUd(UdArgs args) : base(args)
{
    LastScheduleCheckUtcSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("last_schedule_check_utc_seconds", true));
    LastOnlineUtcSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("last_online_utc_seconds", true));
    EventMobsKilled = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("event_mobs_killed", true));
    AdsWatched = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("ads_watched", true));
    PlaytimeMinutes = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("playtime_minutes", true));
    ActiveEventKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("active_event_key", true));
    ActiveScheduleStamp = new global::Multicast.UserData.UdValue<global::System.String>(Child("active_schedule_stamp", true));
    LastCompletedId = new global::Multicast.UserData.UdValue<global::System.String>(Child("last_completed_id", true));
    ReadyStampByEventKey = new global::Multicast.UserData.UdDictValue<global::System.String>(Child("ready_stamp_by_event_key", true));
    LastSkippedStampByEventKey = new global::Multicast.UserData.UdDictValue<global::System.String>(Child("last_skipped_stamp_by_event_key", true));
}
	}
}
