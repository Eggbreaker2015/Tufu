using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BootStepEvent : BaseAnalyticsEvent
	{
		[JsonProperty("step")]
		public string Step { get; }

		[JsonProperty("time")]
		public int Time { get; }

		[JsonIgnore]
		public override string Name => null;

		public BootStepEvent(string step, int time)
		{
		}
	}
}
