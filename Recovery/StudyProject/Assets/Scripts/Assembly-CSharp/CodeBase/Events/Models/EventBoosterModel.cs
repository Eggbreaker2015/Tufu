using System;
using System.Linq;
using Codebase.In_apps;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using Codebase.Analytics.Events;
using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Services.Purchases;
using Codebase.Services.Purchases.Handlers;
using UniMob;
using UniMob.Core;

namespace Codebase.Events.Models
{
	public class EventBoosterModel : Model
	{
		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PurchaseProcessingService purchaseService;

		[Inject]
		private IAnalyticsWrapper analytics;

		[Inject]
		private UserModel userModel;

		private ComputedAtom<int> __PurchaseCount;

		private ComputedAtom<int> __RemainingPurchases;

		private ComputedAtom<bool> __HasRemainingPurchases;

		private ComputedAtom<int> __CurrentGemCost;

		private ComputedAtom<bool> __CanPurchaseGemBooster;

		private ComputedAtom<int> __FreeTokenCount;

		private ComputedAtom<bool> __HasFreeToken;

		private ComputedAtom<bool> __CanPurchaseIAPBooster;

		private ComputedAtom<bool> __CanSpawnMobs;

		private ComputedAtom<int> __TotalAvailableSpawnSlots;

		public EventBoosterDef Def { get; }

		public EventModel Event { get; }

		public bool IsIAP => Def.Type == EventBoosterType.IAP && Def.ProductKey != ProductKey.None;

		public int PurchaseCount
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eb34c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__PurchaseCount == null) __PurchaseCount = CodeGenAtom.CreatePooled(this, null, () => PurchaseCount, false);
                return __PurchaseCount.DirectEvaluate() ? Event.GetBoosterPurchases(Def.Key) : __PurchaseCount.Value;
            }
        }

		public int RemainingPurchases
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eb514.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__RemainingPurchases == null) __RemainingPurchases = CodeGenAtom.CreatePooled(this, null, () => RemainingPurchases, false);
                return __RemainingPurchases.DirectEvaluate() ? (Def.IsUnlimited ? int.MaxValue : Def.MaxPurchases - PurchaseCount) : __RemainingPurchases.Value;
            }
        }

		public bool HasRemainingPurchases
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eb660.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasRemainingPurchases == null) __HasRemainingPurchases = CodeGenAtom.CreatePooled(this, null, () => HasRemainingPurchases, false);
                return __HasRemainingPurchases.DirectEvaluate() ? (Def.IsUnlimited || RemainingPurchases > 0) : __HasRemainingPurchases.Value;
            }
        }

		public int CurrentGemCost
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eb7b4.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentGemCost == null) __CurrentGemCost = CodeGenAtom.CreatePooled(this, null, () => CurrentGemCost, false);
                return __CurrentGemCost.DirectEvaluate() ? Def.BaseGemCost + Def.GemCostIncrement * PurchaseCount : __CurrentGemCost.Value;
            }
        }

		public bool CanPurchaseGemBooster
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eb8f0.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CanPurchaseGemBooster == null) __CanPurchaseGemBooster = CodeGenAtom.CreatePooled(this, null, () => CanPurchaseGemBooster, false);
                return __CanPurchaseGemBooster.DirectEvaluate() ? (Def.Type == EventBoosterType.Gems && HasRemainingPurchases && playerModel.Gems >= CurrentGemCost) : __CanPurchaseGemBooster.Value;
            }
        }

		public int FreeTokenCount
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eba6c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__FreeTokenCount == null) __FreeTokenCount = CodeGenAtom.CreatePooled(this, null, () => FreeTokenCount, false);
                return __FreeTokenCount.DirectEvaluate() ? (IsIAP ? GetFreeTokens() : 0) : __FreeTokenCount.Value;
            }
        }

		public bool HasFreeToken
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14eadc4.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__HasFreeToken == null) __HasFreeToken = CodeGenAtom.CreatePooled(this, null, () => HasFreeToken, false);
                return __HasFreeToken.DirectEvaluate() ? FreeTokenCount > 0 : __HasFreeToken.Value;
            }
        }

		public bool CanPurchaseIAPBooster
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ebc2c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CanPurchaseIAPBooster == null) __CanPurchaseIAPBooster = CodeGenAtom.CreatePooled(this, null, () => CanPurchaseIAPBooster, false);
                return __CanPurchaseIAPBooster.DirectEvaluate() ? (Def.Type == EventBoosterType.IAP && HasRemainingPurchases) : __CanPurchaseIAPBooster.Value;
            }
        }

		public bool CanSpawnMobs
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ebd84.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CanSpawnMobs == null) __CanSpawnMobs = CodeGenAtom.CreatePooled(this, null, () => CanSpawnMobs, false);
                return __CanSpawnMobs.DirectEvaluate() ? ReadCanSpawnMobs() : __CanSpawnMobs.Value;
            }
        }

		public int TotalAvailableSpawnSlots
        {
            get
            {
                // Original APK ARM64 ELF VA 0x14ec24c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__TotalAvailableSpawnSlots == null) __TotalAvailableSpawnSlots = CodeGenAtom.CreatePooled(this, null, () => TotalAvailableSpawnSlots, false);
                return __TotalAvailableSpawnSlots.DirectEvaluate() ? ReadAvailableSpawnSlots() : __TotalAvailableSpawnSlots.Value;
            }
        }

		public EventBoosterModel(Lifetime lifetime, EventBoosterDef def, EventModel eventModel)
			: base(lifetime)
		{
			// Original APK ARM64 ELF VA 0x14eaa8c.
			Def = def;
			Event = eventModel;
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x14eaad0.
			base.Initialize();
			if (IsIAP) PurchasePopupController.OnPurchaseClaimed += OnPurchaseClaimed;
		}

		public override void Dispose()
		{
			// Original APK ARM64 ELF VA 0x14eac30.
			base.Dispose();
			PurchasePopupController.OnPurchaseClaimed -= OnPurchaseClaimed;
		}

		private void OnPurchaseClaimed(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
		{
			// Original APK ARM64 ELF VA 0x14ead60.
			if (IsIAP && rewardInfo.ProductKey == Def.ProductKey && Event.IsActive && HasFreeToken) RedeemFreeToken();
		}

		public void TryPurchase()
		{
			// Original APK ARM64 ELF VA 0x14ec460.
			if (Def.Type == EventBoosterType.Gems) PurchaseGemBooster();
			else if (HasFreeToken) RedeemFreeToken();
			else PurchaseIAPBooster();
		}

		private void PurchaseGemBooster()
		{
			// Original APK ARM64 ELF VA 0x14ec4ac.
			if (!CanPurchaseGemBooster) return;
			int cost = CurrentGemCost;
			playerModel.Gems -= cost;
			analytics.Send(new HardCurrencySpentEvent("event_booster", cost, new Dictionary<string, object>
			{
			    ["event_key"] = Event.Def.Key,
			    ["booster_key"] = Def.Key,
			}));
			int spawned = 0;
			foreach (EnemySpawner spawner in GetEventMobSpawners().OrderBy(item => UnityEngine.Random.value).ToList())
			{
			    while (spawned < Def.SpawnCount && spawner.TrySpawnIgnoringMaxCount()) spawned++;
			    if (spawned >= Def.SpawnCount) break;
			}
			Event.IncrementBoosterPurchases(Def.Key);
			analytics.Send(new ShopBuyEvent("event_booster_" + Def.Key, cost, userModel.Minutes.Value));
		}

		private void PurchaseIAPBooster()
		{
			// Original APK ARM64 ELF VA 0x14ecad8.
			if (CanPurchaseIAPBooster) purchaseService.StartPurchase(Def.ProductKey);
		}

		private void RedeemFreeToken()
		{
			// Original APK ARM64 ELF VA 0x14eaf00.
			string storeId = GameContext.Get<IAPManager>().GetStoreId(Def.ProductKey);
			if (!userModel.ConsumeFreeBoosterToken(storeId)) return;
			foreach (RewardBase reward in Def.Rewards)
			    App.ExecuteCommand(new GrantRewardCommand { Reward = reward, SourcePath = "event.prize" });
			Event.IncrementBoosterPurchases(Def.Key);
			analytics.Send(new ShopBuyEvent("event_booster_" + Def.Key + "_free_token", 0, userModel.Minutes.Value));
		}

		private IEnumerable<EnemySpawner> GetEventMobSpawners()
		{
			// Original APK ARM64 ELF VA 0x14ec0c0.
			string key = eventsRepository.CurrentEvent?.Def.Key;
			if (string.IsNullOrEmpty(key)) return Enumerable.Empty<EnemySpawner>();
			return enemiesModel.Spawners.Where(spawner => spawner.Config.IsEvent && spawner.Config.EventDef != null &&
			    spawner.Config.EventDef.Key == key && spawner.Config.EventDef.UnitType == EventUnitType.Mob);
		}
        private int GetFreeTokens()
        {
            string storeId = GameContext.Get<IAPManager>().GetStoreId(Def.ProductKey);
            return storeId == null ? 0 : userModel.GetFreeBoosterTokens(storeId);
        }

        private bool ReadCanSpawnMobs()
        {
            _ = Event.SpawnerTick;
            return GetEventMobSpawners().Any(spawner => spawner.CanSpawn);
        }

        private int ReadAvailableSpawnSlots()
        {
            _ = Event.SpawnerTick;
            return GetEventMobSpawners().Sum(spawner => spawner.MaxCount - spawner.CurrentCount);
        }

	}
}
