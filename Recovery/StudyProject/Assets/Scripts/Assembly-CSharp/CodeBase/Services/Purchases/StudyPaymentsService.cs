using System;
using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.LevelPass;
using Multicast;
using Multicast.Payments;
using UnityEngine;

namespace Codebase.Services.Purchases
{
    /// <summary>Learning-only sandbox. Business rewards remain in PurchaseProcessingService.</summary>
    public sealed class StudyPaymentsService : IPaymentsService
    {
        private const string ProviderName = "local-study-simulation";
        private readonly UnityIAPManager catalog;
        private bool initialized;
        private bool purchasing;
        private readonly EventSource changed = new EventSource();
        private readonly EventSource<InitiatedPayment> initiated = new EventSource<InitiatedPayment>();
        private readonly EventSource<PendingPayment> pending = new EventSource<PendingPayment>();
        private readonly EventSource<PurchaseArgs> purchased = new EventSource<PurchaseArgs>();
        private readonly EventSource<PaymentFailedException> failed = new EventSource<PaymentFailedException>();
        private readonly EventSource<PaymentCancelledException> cancelled = new EventSource<PaymentCancelledException>();
        private readonly EventSource<ProviderInitArgs> providerInitialized = new EventSource<ProviderInitArgs>();
        private readonly EventSource<ProviderInitFailArgs> providerFailed = new EventSource<ProviderInitFailArgs>();
        public EventStream OnChanged => changed.Stream;
        public EventStream<InitiatedPayment> OnPaymentInitiated => initiated.Stream;
        public EventStream<PendingPayment> OnPaymentPending => pending.Stream;
        public EventStream<PurchaseArgs> OnPaymentPurchased => purchased.Stream;
        public EventStream<PaymentFailedException> OnPaymentFailed => failed.Stream;
        public EventStream<PaymentCancelledException> OnPaymentUserCancelled => cancelled.Stream;
        public EventStream<ProviderInitArgs> OnProviderInitialized => providerInitialized.Stream;
        public EventStream<ProviderInitFailArgs> OnProviderInitFailed => providerFailed.Stream;
        public StoreRegion? Region => null;

        public StudyPaymentsService(UnityIAPManager catalog) => this.catalog = catalog;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            changed.Invoke();
        }

        private bool IsConfigured(string id) => Enum.TryParse(id, out ProductKey key) &&
            Enum.IsDefined(typeof(ProductKey), key) && key != ProductKey.None && key.ToString() == id && catalog.GetProductConfig(key) != null;

        // Local billing adaptation: honor the real profile's current entitlement flags.
        // Consumable gems/event tokens remain repeatable; timed/weekly flags reset in their existing systems.
        private static bool IsAlreadyOwned(string id)
        {
            if (!GameContext.Has<UserModel>()) return false;
            var user = GameContext.Get<UserModel>();
            switch (id)
            {
                case nameof(ProductKey.NoAds): return user.NoAdsPurchased.Value;
                case nameof(ProductKey.SoulPass): return user.SoulPassPurchased.Value;
                case nameof(ProductKey.CrystalPass): return user.CrystalPassPurchased.Value;
                case nameof(ProductKey.ChaoticSoulOffer): return user.ChaoticSoulOfferPurchased.Value;
                default:
                    // Existing entitlement lookup only; this does not implement any tier/reward logic.
                    if (!GameContext.Has<LevelPassConfig>() || !GameContext.Has<LevelPassModel>()) return false;
                    var config = GameContext.Get<LevelPassConfig>();
                    var model = GameContext.Get<LevelPassModel>();
                    if (config.Passes != null)
                        foreach (var pass in config.Passes)
                            if (pass.ProductKey.ToString() == id && model.IsPassPurchased(pass)) return true;
                    return false;
            }
        }

        public bool IsAvailableToPurchase(string id) => initialized && !purchasing && IsConfigured(id) && !IsAlreadyOwned(id);
        public string GetLocalizedPrice(string id) => IsConfigured(id) ? "模拟支付" : string.Empty;
        public string GetStoreSpecificProductId(string id) => IsConfigured(id) ? id : string.Empty;

        public async Awaitable<IReadOnlyList<PurchaseArgs>> ContinueInterruptedPayments()
        {
            // There is no charged external transaction to resume. Delivered IDs live in the profile ledger.
            await Awaitable.NextFrameAsync();
            return Array.Empty<PurchaseArgs>();
        }

        public async Awaitable<PurchaseArgs> Purchase(string productId, Reward[] rewards = null)
        {
            if (!IsAvailableToPurchase(productId))
            {
                var error = new PaymentFailedException(productId, "Local study product unavailable");
                failed.Invoke(error);
                throw error;
            }
            purchasing = true;
            changed.Invoke();
            try
            {
                initiated.Invoke(new InitiatedPayment(productId, ProviderName));
                // Let the existing pending-order UI display before completing the local simulation.
                await Awaitable.WaitForSecondsAsync(0.35f);
                string transaction = "study-" + Guid.NewGuid().ToString("N");
                var receipt = new PaymentReceipt { Store = ProviderName, TransactionID = transaction, Payload = "local simulated success" };
                var confirmed = new ConfirmedPayment(productId, transaction, receipt, productId, string.Empty, 0m, ProviderName);
                var result = new PurchaseArgs(confirmed, transaction, true);
                purchased.Invoke(result);
                return result;
            }
            finally
            {
                purchasing = false;
                changed.Invoke();
            }
        }
    }
}
