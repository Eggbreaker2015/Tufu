using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class OpenShopEvent : BaseAnalyticsEvent
	{
		[JsonProperty("location_number")]
		public int LocationNumber { get; }

		[JsonIgnore]
		public override string Name => "open_shop"; // Native ELF0x15b02b4, original string literal.

		public OpenShopEvent(int locationNumber)
		{
            // Native ELF0x15b02f4.
            LocationNumber = locationNumber;
        }
	}
}
