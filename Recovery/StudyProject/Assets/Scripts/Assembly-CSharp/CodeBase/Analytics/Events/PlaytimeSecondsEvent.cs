using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class PlaytimeSecondsEvent : BaseAnalyticsEvent
	{
		[JsonProperty("seconds")]
		public int PlayTime { get; }

		[JsonIgnore]
		public override string Name => null;

		public PlaytimeSecondsEvent(int playTime)
		{
		}
	}
}
