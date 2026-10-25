using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class HubGuestIdGeneratedEvent : BaseAnalyticsEvent
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("guest_id")]
		public string GuestId { get; }

		public HubGuestIdGeneratedEvent(string guestId)
		{
		}
	}
}
