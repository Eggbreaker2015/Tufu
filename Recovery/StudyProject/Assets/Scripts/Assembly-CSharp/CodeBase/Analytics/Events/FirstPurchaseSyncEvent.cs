using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class FirstPurchaseSyncEvent : BaseAnalyticsEvent
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("rc_user_id")]
		public string RevenueCatUserId { get; }

		public FirstPurchaseSyncEvent(string revenueCatUserId)
		{
		}
	}
}
