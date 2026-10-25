using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Crystals;
using Codebase.Services.Time;
using Codebase.UI.Shop;
using UniMob;
using UniMob.Core;

namespace Codebase.Shop
{
	public class ShopLotRepository : ModelRepository<ShopLotModel, ShopUd>
	{
		[Inject]
		private ShopSectionsConfig config;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private CrystalModel crystalModel;

		private ComputedAtom<List<ShopLotModel>> __Values;

		private ComputedAtom<int> __ActiveLostCount;

		private ComputedAtom<bool> __CanClaimSomething;

		private ComputedAtom<TimeSpan> __TimeToNextFreeClaim;

		public List<ShopLotModel> Values
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Values == null) __Values = CodeGenAtom.CreatePooled(this, null, () => Values, false);
				return __Values.DirectEvaluate() ? values.ToList() : __Values.Value;
			}
		}

		public int ActiveLostCount
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ActiveLostCount == null) __ActiveLostCount = CodeGenAtom.CreatePooled(this, null, () => ActiveLostCount, false);
				return __ActiveLostCount.DirectEvaluate() ? Values.Count(lot => lot.TotalValue > 0.0) : __ActiveLostCount.Value;
			}
		}

		public bool CanClaimSomething
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CanClaimSomething == null) __CanClaimSomething = CodeGenAtom.CreatePooled(this, null, () => CanClaimSomething, false);
				return __CanClaimSomething.DirectEvaluate() ? Values.Any(lot => lot.CanClaim && lot.TotalValue > 0.0 && IsLotAccessible(lot)) : __CanClaimSomething.Value;
			}
		}

		public TimeSpan TimeToNextFreeClaim
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TimeToNextFreeClaim == null) __TimeToNextFreeClaim = CodeGenAtom.CreatePooled(this, null, () => TimeToNextFreeClaim, false);
				if (!__TimeToNextFreeClaim.DirectEvaluate()) return __TimeToNextFreeClaim.Value;
				GameTime now = timeService.Now;
				GameTime? reset = TryReadStoredReset();
				if (!reset.HasValue) return TimeSpan.Zero;
				TimeSpan remaining = reset.Value - now;
				return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
			}
		}

		public ShopLotRepository(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public void Populate()
		{
			bool firstDay = Data.ClaimedLots.Count == 0;
			foreach (ShopSectionDef section in config.Sections)
				foreach (ShopLotDef def in section.Lots)
				{
					var model = new ShopLotModel(Lifetime, def, Data);
					Injector.InjectInto(model);
					values.Add(model);
				}
			if (firstDay) MarkCanClaimOnFirstDayLots();
		}

		private void MarkCanClaimOnFirstDayLots()
		{
			GameContext.Get<GameDataService>();
			foreach (ShopLotModel lot in values)
				if (!lot.Def.CanClaimOnFirstDay) lot.HasClaimed = true;
		}

		public void PopulateFrom(ShopSectionsConfig additionalConfig)
		{
			foreach (ShopSectionDef section in additionalConfig.Sections)
				foreach (ShopLotDef def in section.Lots)
				{
					if (Get(def.Key) != null) continue;
					var model = new ShopLotModel(Lifetime, def, Data);
					Injector.InjectInto(model);
					Add(model);
				}
		}

		public ShopLotModel Get(string key)
		{
			return Values.FirstOrDefault(lot => lot.Def.Key == key);
		}

		private bool IsLotAccessible(ShopLotModel lot)
		{
			if (lot.Def.LotType == ShopLotType.Currency) return crystalModel.IsUnlocked;
			if (lot.Def.LotType == ShopLotType.Soul) return amuletModel.IsUnlocked;
			return true;
		}

		public List<ShopLotModel> GetByType(ShopLotType type)
		{
			return Values.Where(lot => lot.Def.LotType == type).ToList();
		}

		public void ResetFreeClaims()
		{
			foreach (ShopLotModel lot in values)
				if (!lot.IsFreeChestLot) lot.HasClaimed = false;
		}

		public void ResetFreeChestCooldown(string lotKey)
		{
			Get(lotKey)?.ResetFreeChestCooldown();
		}

		public GameTime? TryReadStoredReset()
		{
			string stored = Data.NextFreeClaimReset.Value;
			if (!string.IsNullOrEmpty(stored) && DateTime.TryParseExact(stored, "O", CultureInfo.InvariantCulture,
				DateTimeStyles.RoundtripKind, out DateTime parsed)) return GameTime.FromDateTime(parsed.ToUniversalTime());
			return null;
		}

		public void SetNextFreeClaimReset(GameTime time)
		{
			Data.NextFreeClaimReset.Value = time.ToDateTime().ToUniversalTime().ToString("O");
		}
	}
}
