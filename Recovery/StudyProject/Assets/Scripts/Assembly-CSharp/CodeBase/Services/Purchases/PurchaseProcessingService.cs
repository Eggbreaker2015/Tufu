using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Events;
using Codebase.In_apps;
using Codebase.LevelPass;
using Codebase.Player;
using Codebase.Rewards;
using Codebase.Rewards.Flows;
using Codebase.Services.Purchases.Handlers;
using Codebase.UI.Shop;
using Cysharp.Threading.Tasks;
using Multicast.Payments;
using UnityEngine;

namespace Codebase.Services.Purchases
{
    public class PurchaseProcessingService
    {
        private readonly List<IProductHandler> handlers = new List<IProductHandler>();
        private readonly IAPManager iapManager;
        private readonly IPaymentsService paymentsService;
        private bool purchaseInProgress;
        private readonly HashSet<string> deliveringTransactions = new HashSet<string>();

        public event Action OnPurchaseStarted;
        public event Action<PurchaseDeliveryInfo, PurchaseRewardInfo> OnPurchaseDelivered;
        public event Action OnPurchaseFinished;
        public event Action<(bool userCancelled, ProductKey productKey)> OnPurchaseFailed;

        // Original ARM64 ELF VA 0x1405df4: same handler registration, reward factories, and icon mapping.
        public PurchaseProcessingService(ShopSectionsConfig shopConfig, EventsProvider eventsProvider,
            LevelPassConfig levelPassConfig, IAPManager iapManager, IconStyleConfig iconStyleConfig,
            IPaymentsService paymentsService)
        {
            this.iapManager = iapManager;
            this.paymentsService = paymentsService;
            handlers.Add(new GemsProductHandler().RegisterFromConfig(shopConfig));
            handlers.Add(new EventBoosterProductHandler().RegisterFromConfig(eventsProvider, iapManager));
            handlers.Add(new NoAdsProductHandler(ProductKey.NoAds, iconStyleConfig.NoAdsSprite));
            handlers.Add(new OfferProductHandler(ProductKey.SoulPass, iconStyleConfig.SoulPassSprite, () => new SoulPassReward()));
            handlers.Add(new OfferProductHandler(ProductKey.CrystalPass, iconStyleConfig.CrystalPassSprite, () => new CrystalPassReward()));
            handlers.Add(new OfferProductHandler(ProductKey.ChaoticSoulOffer, iconStyleConfig.ChaoticSoulOfferSprite, () => new ChaoticSoulOfferReward()));
            handlers.Add(new LevelPassProductHandler().RegisterFromConfig(levelPassConfig, iapManager, iconStyleConfig.LevelPassSprite));
        }

        // Original ARM64 ELF VA 0x140775c: deliver each resumed order once.
        public async UniTask ContinueInterruptedPayments()
        {
            var orders = await paymentsService.ContinueInterruptedPayments();
            foreach (var order in orders) await DeliverOrderAsync(order);
        }

        // Original ARM64 ELF VA 0x13fd76c.
        public void StartPurchase(ProductKey productKey) => StartPurchaseInternalAsync(productKey).Forget();

        public async UniTask StartPurchaseInternalAsync(ProductKey productKey)
        {
            // Local learning adaptation: serialize clicks while the simulated payment is outstanding.
            if (purchaseInProgress) return;
            purchaseInProgress = true;
            try
            {
                var handler = handlers.FirstOrDefault(value => value.CanHandle(productKey));
                var config = iapManager.GetProductConfig(productKey);
                if (config == null || handler == null || !handler.GetRewardInfo(productKey, 1).HasValue)
                    throw new InvalidOperationException("No configured purchase handler for " + productKey);
                if (!iapManager.IsAvailableToPurchase(productKey))
                    throw new InvalidOperationException("Product is unavailable: " + productKey);
                // Original 0x14086f8 builds handler rewards + positive inAppValue as VipPointsReward.
                BuildRewards(handler, productKey, config);
                OnPurchaseStarted?.Invoke();
                // Local learning adaptation: no server SKU envelope or billing SDK is required.
                // The real handler rewards are delivered below through the game's reward flow.
                var order = await paymentsService.Purchase(productKey.ToString());
                if (order == null || order.ConfirmedPayment == null || order.ConfirmedPayment.ProductId != productKey.ToString())
                    throw new InvalidOperationException("Payment result does not match requested product " + productKey);
                await DeliverOrderAsync(order);
            }
            catch (PaymentCancelledException)
            {
                OnPurchaseFailed?.Invoke((true, productKey));
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                OnPurchaseFailed?.Invoke((false, productKey));
            }
            finally
            {
                purchaseInProgress = false;
                OnPurchaseFinished?.Invoke();
            }
        }

        private static List<RewardBase> BuildRewards(IProductHandler handler, ProductKey key, ProductConfig config)
        {
            var rewards = new List<RewardBase>();
            handler.BuildRewards(rewards, key);
            if (rewards.Count == 0 || rewards.Any(reward => reward == null))
                throw new InvalidOperationException("Product has no valid configured rewards: " + key);
            if (config.inAppValue > 0) rewards.Add(new VipPointsReward { Amount = config.inAppValue });
            return rewards;
        }

        public async UniTask DeliverOrderAsync(PurchaseArgs order)
        {
            // Original 0x1407f94 validates ProductKey and resolves the product's reward info.
            // Local learning adaptation: directly grant the same handler rewards, replacing the backend mail claim.
            if (order == null || order.ConfirmedPayment == null)
                throw new ArgumentException("Missing confirmed payment", nameof(order));
            var payment = order.ConfirmedPayment;
            if (!Enum.TryParse(payment.ProductId, out ProductKey key) || !Enum.IsDefined(typeof(ProductKey), key) || key == ProductKey.None)
                throw new InvalidOperationException("Unknown purchase product: " + payment.ProductId);
            if (string.IsNullOrEmpty(payment.TransactionId))
                throw new InvalidOperationException("Confirmed payment has no transaction ID");
            var handler = handlers.FirstOrDefault(value => value.CanHandle(key));
            var config = iapManager.GetProductConfig(key);
            var rewardInfo = handler?.GetRewardInfo(key, 1);
            if (config == null || handler == null || !rewardInfo.HasValue)
                throw new InvalidOperationException("No configured delivery handler for " + key);
            var save = GameContext.Get<GameDataService>();
            var ledger = save.Data.User.LocalPurchaseTransactions;
            if (ledger.TryGetValue(payment.TransactionId, out var deliveredProduct))
            {
                if (deliveredProduct != payment.ProductId)
                    throw new InvalidOperationException("Transaction ID belongs to another product");
                return;
            }
            if (!deliveringTransactions.Add(payment.TransactionId)) return;
            try
            {
                // All registered product handlers grant non-chest rewards synchronously before this
                // shared flow's first await. Commit delivery before its presentation delay can be stopped.
                UniTask delivery = App.InitiateFlow(new GrantRewardsFlow
                {
                    Rewards = BuildRewards(handler, key, config),
                    SourcePath = "shop"
                });
                if (delivery.Status == UniTaskStatus.Faulted || delivery.Status == UniTaskStatus.Canceled)
                    await delivery;
                // Local learning ledger and the actual rewards share this profile save boundary.
                ledger[payment.TransactionId] = payment.ProductId;
                save.ForceSave();
                await delivery;
                OnPurchaseDelivered?.Invoke(new PurchaseDeliveryInfo(key, payment.TransactionId), rewardInfo.Value);
            }
            finally { deliveringTransactions.Remove(payment.TransactionId); }
        }

        private void SendRevenueToFirebase(PurchaseArgs order)
        {
            // Local simulated purchases do not report real revenue to third-party services.
        }
    }
}
