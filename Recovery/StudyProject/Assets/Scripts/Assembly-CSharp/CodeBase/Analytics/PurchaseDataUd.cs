using Multicast.UserData;

namespace Codebase.Analytics
{
	public class PurchaseDataUd : UdObject
	{
		public UdValue<string> ProductId { get; }

		public UdValue<string> Receipt { get; }

		public UdValue<string> TransactionId { get; }

		public PurchaseDataUd(UdArgs args) : base(args)
{
    ProductId = new global::Multicast.UserData.UdValue<global::System.String>(Child("product_id", true));
    Receipt = new global::Multicast.UserData.UdValue<global::System.String>(Child("receipt", true));
    TransactionId = new global::Multicast.UserData.UdValue<global::System.String>(Child("transaction_id", true));
}
	}
}
