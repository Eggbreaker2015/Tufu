using System;
using Codebase.Core;
using Codebase.In_apps;
using Codebase.Services.Purchases.Handlers;
using Codebase.UI;
using UnityEngine;

namespace Codebase.Services.Purchases
{
	public class PurchasePopupController
	{
		public static Action<PurchaseDeliveryInfo, PurchaseRewardInfo> OnPurchaseClaimed;

		private readonly PurchaseProcessingService purchaseService;

		private PendingOrderView pendingOrderPopup;

		public PurchasePopupController(PurchaseProcessingService purchaseService)
		{
		}

		private void HandlePurchaseStarted()
		{
		}

		private void HandlePurchaseDelivered(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
		{
		}

		private void ShowRewardPopup(Sprite icon, string amount, Action callback)
		{
		}

		private void HandlePurchaseFinished()
		{
		}

		private void HandlePurchaseFailed((bool userCancelled, ProductKey productKey) failed)
		{
		}

		private void HandleTimeout()
		{
		}

		[StaticReset]
		private static void ResetStaticState()
		{
		}
	}
}
