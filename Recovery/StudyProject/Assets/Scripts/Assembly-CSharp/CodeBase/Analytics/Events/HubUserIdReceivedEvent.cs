using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class HubUserIdReceivedEvent : BaseAnalyticsEvent
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("user_id")]
		public string UserId { get; }

		public HubUserIdReceivedEvent(string userId)
		{
		}
	}
}
