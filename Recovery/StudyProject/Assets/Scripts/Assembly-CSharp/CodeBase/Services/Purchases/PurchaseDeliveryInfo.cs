using Codebase.In_apps;

namespace Codebase.Services.Purchases
{
    public class PurchaseDeliveryInfo
    {
        public ProductKey ProductKey { get; }
        public string MailKey { get; }
        // Original ARM64 ELF VA 0x14086b4. Local delivery uses the transaction key in place of a backend mail key.
        public PurchaseDeliveryInfo(ProductKey productKey, string mailKey)
        {
            ProductKey = productKey;
            MailKey = mailKey;
        }
    }
}
