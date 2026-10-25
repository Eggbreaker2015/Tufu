using System;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.In_apps;
using Codebase.Services.Purchases.Handlers;
using Codebase.UI;
using Codebase.Core.UI;
using UnityEngine;

namespace Codebase.Services.Purchases
{
    public class PurchasePopupController
    {
        public static Action<PurchaseDeliveryInfo, PurchaseRewardInfo> OnPurchaseClaimed;
        private readonly PurchaseProcessingService purchaseService;
        private PendingOrderView pendingOrderPopup;

        // Original ARM64 ELF VA 0x1404b78: same four event subscriptions.
        public PurchasePopupController(PurchaseProcessingService purchaseService)
        {
            this.purchaseService = purchaseService;
            purchaseService.OnPurchaseStarted += HandlePurchaseStarted;
            purchaseService.OnPurchaseDelivered += HandlePurchaseDelivered;
            purchaseService.OnPurchaseFinished += HandlePurchaseFinished;
            purchaseService.OnPurchaseFailed += HandlePurchaseFailed;
        }
        private void HandlePurchaseStarted()
        {
            if (pendingOrderPopup != null) return;
            pendingOrderPopup = GameContext.Get<UINavigator>().Open<PendingOrderView>(UIConstants.PendingOrderView, true);
            pendingOrderPopup.SetUserCloseCallback(() => pendingOrderPopup = null);
            pendingOrderPopup.SetTimeoutCallback(HandleTimeout);
        }
        private void HandlePurchaseDelivered(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
        {
            // Local learning adaptation: service already granted rewards and saved, so no backend mail is claimed here.
            // A restored interrupted local order can also show its receipt without requiring an old pending popup.
            ShowRewardPopup(rewardInfo.Icon, rewardInfo.Amount, () =>
            {
                OnPurchaseClaimed?.Invoke(deliveryInfo, rewardInfo);
                GameContext.Get<GameDataService>().ForceSave();
            });
        }
        private void ShowRewardPopup(Sprite icon, string amount, Action callback)
        {
            var popup = GameContext.Get<UINavigator>().Open<PurchaseRewardView>(UIConstants.PurchaseRewardView, true);
            popup.Setup(icon, amount);
            popup.SetUserCloseCallback(callback);
        }
        private void HandlePurchaseFinished()
        {
            if (pendingOrderPopup == null) return;
            var popup = pendingOrderPopup;
            pendingOrderPopup = null;
            popup.Close();
        }
        private void HandlePurchaseFailed((bool userCancelled, ProductKey productKey) failed)
        {
            // User requested no mailbox UI; failures remain in the original pending-order state.
            if (pendingOrderPopup != null) pendingOrderPopup.ShowFailed();
        }
        private void HandleTimeout()
        {
            if (pendingOrderPopup != null) pendingOrderPopup.ShowPending();
        }
        [StaticReset] private static void ResetStaticState() => OnPurchaseClaimed = null;
    }
}
