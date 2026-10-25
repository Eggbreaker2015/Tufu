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

		public PurchaseRewardInfo(Sprite icon, string amount, ProductKey productKey, int quantity = 1)
		{
			Icon = null;
			Amount = null;
			ProductKey = ProductKey.None;
			Quantity = 0;
		}
	}
}
