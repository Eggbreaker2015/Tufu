using Newtonsoft.Json;

namespace Codebase.Analytics
{
	public interface IUserContextEvent
	{
		[JsonProperty("days_since_install")]
		int DaysSinceInstall { get; }

		[JsonProperty("playtime_minutes")]
		int PlaytimeMinutes { get; }

		[JsonProperty("last_opened_island")]
		int LastOpenedIsland { get; }
	}
}
