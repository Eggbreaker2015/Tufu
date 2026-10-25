using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class ActivePlaytimeEvent : BaseAnalyticsEvent
	{
		[JsonProperty("minutes")]
		public int PlayTime { get; }

		[JsonIgnore]
		public override string Name => null;

		public ActivePlaytimeEvent(int playTime)
		{
		}
	}
}
