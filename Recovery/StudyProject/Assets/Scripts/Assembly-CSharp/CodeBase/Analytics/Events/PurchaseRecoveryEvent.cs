using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class PurchaseRecoveryEvent : BaseAnalyticsEvent
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("product_id")]
		public string ProductId { get; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; }

		public PurchaseRecoveryEvent(string productId, string transactionId)
		{
		}
	}
}
