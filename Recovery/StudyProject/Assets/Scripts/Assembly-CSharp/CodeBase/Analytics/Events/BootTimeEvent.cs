using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BootTimeEvent : BaseAnalyticsEvent
	{
		[JsonProperty("time")]
		public int Time { get; }

		[JsonIgnore]
		public override string Name => null;

		public BootTimeEvent(int time)
		{
		}
	}
}
