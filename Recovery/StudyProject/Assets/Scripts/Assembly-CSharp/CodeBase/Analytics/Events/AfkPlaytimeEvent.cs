using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class AfkPlaytimeEvent : BaseAnalyticsEvent
	{
		[JsonProperty("minutes")]
		public int FakePlayTime { get; }

		[JsonIgnore]
		public override string Name => null;

		public AfkPlaytimeEvent(int fakePlayTime)
		{
		}
	}
}
