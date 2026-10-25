using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class GameLoadedEvent : BaseAnalyticsEvent
	{
		[JsonIgnore]
		public override string Name => null;
	}
}
