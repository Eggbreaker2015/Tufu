using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class AdRevenueEvent : UserContextEventBase
	{
		[JsonProperty("total_impressions")]
		public int TotalImpressions { get; }

		[JsonProperty("total_revenue")]
		public float TotalRevenue { get; }

		[JsonProperty("total_ecpm")]
		public float TotalEcpm { get; }

		[JsonProperty("revenue")]
		public double Revenue { get; }

		[JsonProperty("density")]
		public double Density { get; }

		[JsonProperty("ad_type")]
		public string AdType { get; }

		[JsonIgnore]
		public override string Name => null;

		public AdRevenueEvent(int totalImpressions, float totalRevenue, float totalEcpm, double revenue, double density, string adType = "rewarded")
		{
		}
	}
}
