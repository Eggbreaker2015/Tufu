using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BannerRevenueEvent : UserContextEventBase
	{
		[JsonProperty("total_impressions")]
		public int TotalImpressions { get; }

		[JsonProperty("total_revenue")]
		public float TotalRevenue { get; }

		[JsonProperty("total_ecpm")]
		public float TotalEcpm { get; }

		[JsonProperty("revenue")]
		public double Revenue { get; }

		[JsonIgnore]
		public override string Name => null;

		public BannerRevenueEvent(int totalImpressions, float totalRevenue, float totalEcpm, double revenue)
		{
		}
	}
}
