using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class UserConsentEvent : BaseAnalyticsEvent
	{
		[JsonProperty("gdpr_applies")]
		public bool GdprApplies { get; }

		[JsonProperty("has_user_consent")]
		public bool HasUserConsent { get; }

		[JsonProperty("do_not_sell")]
		public bool IsDoNotSell { get; }

		[JsonProperty("att_authorized")]
		public bool IsAppTrackingAllowed { get; }

		[JsonIgnore]
		public override string Name => null;

		public UserConsentEvent(bool gdprApplies, bool hasUserConsent, bool isDoNotSell, bool isAppTrackingAllowed)
		{
		}
	}
}
