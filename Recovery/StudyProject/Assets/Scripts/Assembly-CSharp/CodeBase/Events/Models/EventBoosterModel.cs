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

		public bool IsIAP => false;

		public int PurchaseCount => 0;

		public int RemainingPurchases => 0;

		public bool HasRemainingPurchases => false;

		public int CurrentGemCost => 0;

		public bool CanPurchaseGemBooster => false;

		public int FreeTokenCount => 0;

		public bool HasFreeToken => false;

		public bool CanPurchaseIAPBooster => false;

		public bool CanSpawnMobs => false;

		public int TotalAvailableSpawnSlots => 0;

		public EventBoosterModel(Lifetime lifetime, EventBoosterDef def, EventModel eventModel)
			: base(default)
		{
		}

		public override void Initialize()
		{
		}

		public override void Dispose()
		{
		}

		private void OnPurchaseClaimed(PurchaseDeliveryInfo deliveryInfo, PurchaseRewardInfo rewardInfo)
		{
		}

		public void TryPurchase()
		{
		}

		private void PurchaseGemBooster()
		{
		}

		private void PurchaseIAPBooster()
		{
		}

		private void RedeemFreeToken()
		{
		}

		private IEnumerable<EnemySpawner> GetEventMobSpawners()
		{
			return null;
		}
	}
}
