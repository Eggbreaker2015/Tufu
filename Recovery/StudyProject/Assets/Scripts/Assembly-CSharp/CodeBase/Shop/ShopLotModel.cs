using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Analytics.Events;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Economy;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Services.Cheats;
using Codebase.Services.Purchases;
using Codebase.Services.Time;
using Codebase.UI.Shop;
using Codebase.Unit;
using UniMob;
using UniMob.Core;

namespace Codebase.Shop
{
	public class ShopLotModel : Model
	{
		[CompilerGenerated]
		private double __RPM__BackingField;

		[Inject]
		private UserModel userModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PurchaseProcessingService purchaseService;

		[Inject]
		private IAnalyticsWrapper analytics;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private ChestsConfig chestsConfig;

		[Inject]
		private CashbackModel cashbackModel;

		[Inject]
		private ChestQueueModel chestQueue;

		[Inject]
		private CurrencyModel currencyModel;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private ABTestService abTestService;

		private ShopUd shopUd;

		private double? scaledTotalValue;

		private double? scaledCost;

		private ComputedAtom<bool> __HasClaimed;

		private ComputedAtom<bool> __CanClaim;

		private ComputedAtom<bool> __CanPurchase;

		private ComputedAtom<bool> __FreeChestReady;

		private ComputedAtom<double> __TotalValue;

		private ComputedAtom<double> __RPM;

		public string Key => Def.Key;

		public bool HasClaimed
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasClaimed == null) __HasClaimed = CodeGenAtom.CreatePooled(this, null, () => HasClaimed, false);
				return __HasClaimed.DirectEvaluate() ? shopUd.ClaimedLots.TryGetValue(Def.Key, out bool claimed) && claimed : __HasClaimed.Value;
			}
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasClaimed != null && !__HasClaimed.CompareAndInvalidate(value)) return;
				shopUd.ClaimedLots[Def.Key] = value;
			}
		}

		public bool CanClaim
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CanClaim == null) __CanClaim = CodeGenAtom.CreatePooled(this, null, () => CanClaim, false);
				return __CanClaim.DirectEvaluate() ? (Def.IsFree && !HasClaimed) || (IsFreeChestLot && FreeChestReady) : __CanClaim.Value;
			}
		}

		public bool CanPurchase
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CanPurchase == null) __CanPurchase = CodeGenAtom.CreatePooled(this, null, () => CanPurchase, false);
				return __CanPurchase.DirectEvaluate() ? CanClaim || playerModel.Gems >= GemCost : __CanPurchase.Value;
			}
		}

		public double GemCost => scaledCost ?? Def.Cost;

		private bool FreeChestReady
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__FreeChestReady == null) __FreeChestReady = CodeGenAtom.CreatePooled(this, null, () => FreeChestReady, false);
				if (!__FreeChestReady.DirectEvaluate()) return __FreeChestReady.Value;
				if (!IsFreeChestLot) return false;
				GameTime? reset = ReadFreeChestReset();
				return !reset.HasValue || !timeService.InFuture(reset.Value);
			}
		}

		public bool IsFreeChestLot => Def.LotType == ShopLotType.Chest && FreeChestCooldownDays > 0;

		private int FreeChestCooldownDays
		{
			get
			{
				double days = metaStatsModel.Get("free_chest").GetValue(Def.Key);
				// Original ARM64 FCVTZS saturates; its explicit +Infinity branch returns MinValue.
				return double.IsNaN(days) ? 0 : double.IsPositiveInfinity(days) ? int.MinValue :
					days >= int.MaxValue ? int.MaxValue : days <= int.MinValue ? int.MinValue : (int)days;
			}
		}

		private TimeSpan FreeChestCooldown => TimeSpan.FromDays(FreeChestCooldownDays);

		public double TotalValue
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TotalValue == null) __TotalValue = CodeGenAtom.CreatePooled(this, null, () => TotalValue, false);
				if (!__TotalValue.DirectEvaluate()) return __TotalValue.Value;
				int locationNumber = playerModel.LastUnlockedLocation.Number;
				if (scaledTotalValue.HasValue) return scaledTotalValue.Value;
				if (Def.LotType == ShopLotType.Soul && !amuletModel.IsSoulDiscovered(Def.SoulKey)) return 0.0;
				switch (Def.LotType)
				{
					case ShopLotType.Gems:
					case ShopLotType.Chest:
					case ShopLotType.Currency:
						return GetCurrencyValue() * GetCurrencyValueMultiplier(locationNumber);
					case ShopLotType.Soul:
						return GetSoulValue() * (double)abTestService.GetSoulValueMultiplier(locationNumber);
					default:
						if (playerModel.LastUnlockedLocation.Number < Def.RequiredLocationNumber) return 0.0;
						double value = Def.Value * RPM;
						if (CanClaim) value /= 25.0;
						// The original FCMP/FCCMP/FCSEL raises only positive fractions to one.
						if (value > 0.0 && value < 1.0) value = 1.0;
						return Math.Ceiling(value) * (double)abTestService.GetInAppStatValueMultiplier(locationNumber) *
							(double)abTestService.GetInAppRpmStatValueMultiplier(Def.StatType);
				}
			}
		}

		public double RPM
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__RPM == null) __RPM = CodeGenAtom.CreatePooled(this, null, () => RPM, false);
				return __RPM.DirectEvaluate() ? __RPM__BackingField : __RPM.Value;
			}
			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__RPM != null && !__RPM.CompareAndInvalidate(value)) return;
				__RPM__BackingField = value;
			}
		}

		public ShopLotDef Def { get; }

		public ShopLotModel(Lifetime lifetime, ShopLotDef def, ShopUd shopUd)
			: base(lifetime)
		{
			Def = def;
			this.shopUd = shopUd;
		}

		public void SetScaledValues(double totalValue, double cost)
		{
			scaledTotalValue = totalValue;
			scaledCost = cost;
		}

		public void Purchase()
		{
			// Recovered original local branches. Gems delegates to the existing platform service.
			if (Def.LotType == ShopLotType.Gems)
			{
				purchaseService.StartPurchase(Def.ProductKey);
				return;
			}
			bool canClaim = CanClaim;
			if (Def.LotType == ShopLotType.Currency || Def.LotType == ShopLotType.Soul)
			{
				bool soul = Def.LotType == ShopLotType.Soul;
				if (!canClaim)
				{
					if (playerModel.Gems < GemCost) return;
					playerModel.Gems -= GemCost;
					ReportHardCurrencySpent(soul ? "shop_soul" : "shop_currency", GemCost,
						new Dictionary<string, object> { ["lot_key"] = Def.Key, [soul ? "soul_key" : "currency_key"] = soul ? Def.SoulKey : Def.CurrencyKey });
				}
				if (soul) amuletModel.AddSouls(Def.SoulKey, TotalValue);
				else currencyModel.Add(Def.CurrencyKey, TotalValue);
				audioPlayer.PlaySoundOnce("shop_souls_buy");
				if (canClaim) analytics.Send(new ShopClaimEvent(Def.Key, TotalValue, userModel.Minutes.Value));
				else analytics.Send(new ShopBuyEvent(Def.Key, TotalValue, userModel.Minutes.Value));
				if (!scaledTotalValue.HasValue) HasClaimed = true;
				return;
			}
			if (!CanPurchase) return;
			if (Def.LotType == ShopLotType.Chest)
			{
				if (!CanClaim)
				{
					playerModel.Gems -= Def.Cost;
					ReportHardCurrencySpent("shop_chest", Def.Cost,
						new Dictionary<string, object> { ["lot_key"] = Def.Key, ["chest_key"] = Def.ChestKey });
				}
				double value = TotalValue;
				int count = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue :
					value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
				chestQueue.EnqueueChest(chestsConfig.Get(Def.ChestKey), count);
				App.RequestAppUpdateFlow();
				if (canClaim)
				{
					analytics.Send(new ShopClaimEvent(Def.Key, count, userModel.Minutes.Value));
					WriteFreeChestReset();
				}
				else analytics.Send(new ShopBuyEvent(Def.Key, count, userModel.Minutes.Value));
				HasClaimed = true;
				return;
			}
			if (!canClaim)
			{
				playerModel.Gems -= Def.Cost;
				ReportHardCurrencySpent("shop_stat", Def.Cost,
					new Dictionary<string, object> { ["lot_key"] = Def.Key, ["stat_type"] = Def.StatType.ToString() });
			}
			var modifier = new StatModifier { Name = "shop", StatType = Def.StatType,
				ModifierType = ModifierType.Flat, Source = ModifierSource.Shop,
				Value = (float)TotalValue * (float)cheatsModel.DropMultiplier };
			playerModel.Unit.Stats.First(stat => stat.Type == Def.StatType).AddModifier(modifier);
			if (canClaim)
			{
				cashbackModel.AddEarnedStat(modifier);
				analytics.Send(new ShopClaimEvent(Def.StatType.ToString(), TotalValue, userModel.Minutes.Value));
			}
			else
			{
				var reward = new StatReward { Type = Def.StatType, Value = modifier.Value,
					BakedData = new BakedStatReward { Type = Def.StatType, Value = modifier.Value } };
				chestQueue.EnqueueRewards(new List<RewardBase> { reward }, "general");
				App.RequestAppUpdateFlow();
				analytics.Send(new ShopBuyEvent(Def.StatType.ToString(), TotalValue, userModel.Minutes.Value));
			}
			HasClaimed = true;
		}

		private void ReportHardCurrencySpent(string source, double amount, Dictionary<string, object> details)
		{
			analytics.Send(new HardCurrencySpentEvent(source, amount, details));
		}

		private double GetCurrencyValue()
		{
			if (Def.CurrencyKey != "crystal.magic_dust") return Def.Value;
			double value = CanClaim ? Def.FreeValue : Def.Value;
			value = value * GetExpectedCrystalDustValue() * (double)abTestService.GetCrystalPassInAppMagicDustMultiplier();
			// Original inline modf/half-even code also retains its large-value and signed-zero behavior.
			double integral = Math.Truncate(value);
			double fraction = double.IsInfinity(value) ? 0.0 : value - integral;
			if (value >= 0.0)
			{
				if (fraction != 0.5) return Math.Floor(value + 0.5);
				return (((long)integral & 1L) == 0) ? integral : integral + 1.0;
			}
			if (fraction != -0.5) return Math.Ceiling(value - 0.5);
			return (((long)integral & 1L) == 0) ? integral : integral - 1.0;
		}

		private double GetCurrencyValueMultiplier(int locationNumber)
		{
			return Def.CurrencyKey == "crystal.magic_dust" ? (double)abTestService.GetCrystalDustValueMultiplier(locationNumber) : 1.0;
		}

		private double GetSoulValue()
		{
			double value = Def.Value * RPM;
			if (CanClaim) value /= 25.0;
			if (value > 0.0 && value < 1.0) value = 1.0;
			double integral = Math.Truncate(value);
			double fraction = double.IsInfinity(value) ? 0.0 : value - integral;
			if (value >= 0.0)
			{
				if (fraction != 0.5) return Math.Floor(value + 0.5);
				return (((long)integral & 1L) == 0) ? integral : integral + 1.0;
			}
			if (fraction != -0.5) return Math.Ceiling(value - 0.5);
			return (((long)integral & 1L) == 0) ? integral : integral - 1.0;
		}

		private double GetExpectedCrystalDustValue()
		{
			return crystalModel.CalculateExpectedDustPerCrystal();
		}

		public void ResetFreeChestCooldown()
		{
			if (IsFreeChestLot) shopUd.FreeChestClaimReset.Remove(Def.Key);
		}

		private GameTime? ReadFreeChestReset()
		{
			if (shopUd.FreeChestClaimReset.TryGetValue(Def.Key, out string stored) && !string.IsNullOrEmpty(stored) &&
				DateTime.TryParseExact(stored, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
				return GameTime.FromDateTime(parsed.ToUniversalTime());
			return null;
		}

		private void WriteFreeChestReset()
		{
			shopUd.FreeChestClaimReset[Def.Key] = timeService.Now.Add(FreeChestCooldown).ToDateTime().ToUniversalTime().ToString("O");
		}
	}
}
