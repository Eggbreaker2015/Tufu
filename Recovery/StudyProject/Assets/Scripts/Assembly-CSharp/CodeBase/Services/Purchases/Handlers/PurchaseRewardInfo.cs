using Codebase.In_apps;
using UnityEngine;

namespace Codebase.Services.Purchases.Handlers
{
    public readonly struct PurchaseRewardInfo
    {
        public Sprite Icon { get; }
        public string Amount { get; }
        public ProductKey ProductKey { get; }
        public int Quantity { get; }
        // Original ARM64 ELF VA 0x1409a8c.
        public PurchaseRewardInfo(Sprite icon, string amount, ProductKey productKey, int quantity)
        {
            Icon = icon;
            Amount = amount;
            ProductKey = productKey;
            Quantity = quantity;
        }
    }
}
