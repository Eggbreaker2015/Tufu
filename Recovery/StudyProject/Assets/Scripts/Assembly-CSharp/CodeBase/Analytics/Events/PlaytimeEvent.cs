using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class PlaytimeEvent : BaseAnalyticsEvent
	{
		[JsonProperty("playTime")]
		public int PlayTime { get; }

		[JsonIgnore]
		public override string Name => null;

		public PlaytimeEvent(int playTime)
		{
		}
	}
}
