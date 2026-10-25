using Newtonsoft.Json;

namespace Codebase.Analytics
{
	public abstract class UserContextEventBase : BaseAnalyticsEvent, IUserContextEvent
	{
		[JsonProperty("days_since_install")]
		public int DaysSinceInstall { get; }

		[JsonProperty("playtime_minutes")]
		public int PlaytimeMinutes { get; }

		[JsonProperty("last_opened_island")]
		public int LastOpenedIsland { get; }
	}
}
