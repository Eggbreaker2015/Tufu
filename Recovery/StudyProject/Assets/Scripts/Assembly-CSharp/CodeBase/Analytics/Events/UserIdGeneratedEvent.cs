using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class UserIdGeneratedEvent : BaseAnalyticsEvent
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("user_id")]
		public string UserId { get; }

		public UserIdGeneratedEvent(string userId)
		{
		}
	}
}
