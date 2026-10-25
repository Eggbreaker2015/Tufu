using Codebase.In_apps;

namespace Codebase.Services.Purchases
{
	public class PurchaseDeliveryInfo
	{
		public ProductKey ProductKey { get; }

		public string MailKey { get; }

		public PurchaseDeliveryInfo(ProductKey productKey, string mailKey)
		{
		}
	}
}
