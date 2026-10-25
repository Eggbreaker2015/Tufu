using System;
using System.Collections.Generic;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Crystals.Objective;
using Codebase.Economy;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Meta.Upgrades;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Codebase.Simulation;
using Codebase.UI;
using Codebase.Unit;
using Codebase.VIP;
using UniMob;
using UniMob.Core;
using System.Linq;

namespace Codebase.Crystals
{
	public class CrystalModel : ModelRepository<CrystalItemModel, CrystalUd>
	{
		[Inject]
		private CrystalConfig config;
		[Inject]
		private AmuletModel amuletModel;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private CurrencyModel currencyModel;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private SimulationConfig simulationConfig;
		[Inject]
		private CheatsModel cheatsModel;
		[Inject]
		private VipModel vipModel;
		[Inject]
		private UserModel userModel;
		[Inject]
		private StatRewardContainer statRewardContainer;
		[Inject]
		private IAnalyticsWrapper analytics;
		private float objectiveBannerShowAtUnscaledTime;
		private bool pendingCrystalRespawnedPopup;
		private const int GuaranteedEpicGeodeOpenIndex = 2;
		private const int GuaranteedLegendaryGeodeOpenIndex = 19;
		public const int ObjectiveBlacksmithKillReward = 3;
		public const float ObjectiveBannerShowDelaySeconds = 1.5f;
		private readonly Dictionary<int, List<(StatBase stat, StatModifier mod)>> appliedModifiersByCrystalId;
		private readonly Random random;
		private ComputedAtom<bool> __IsUnlocked;
		private ComputedAtom<int> __ObjectiveGeodesProgress;
		private ComputedAtom<int> __ObjectiveKillCount;
		private ComputedAtom<bool> __IsObjectiveWaitingForRespawn;
		private ComputedAtom<bool> __IsWaitingForRespawn;
		private ComputedAtom<List<CrystalItemModel>> __Crystals;
		private ComputedAtom<double> __GeodesCount;
		private ComputedAtom<double> __EnchantmentStonesCount;
		private ComputedAtom<double> __MagicDust;
		private ComputedAtom<int> __GuaranteeEnchantThreshold;
		private ComputedAtom<bool> __IsGuaranteeEnchantUnlocked;
		private ComputedAtom<int> __GuaranteeEnchantProgress;
		private ComputedAtom<bool> __IsGuaranteeEnchantReady;
		private ComputedAtom<int> __MaxGeodes;
		private ComputedAtom<int> __GeodeTimerMinutes;
		private ComputedAtom<int> __MaxEnchantmentStones;
		private ComputedAtom<float> __SellPriceBonusPercent;
		private ComputedAtom<float> __CrystalsQualityBonus;
		private ComputedAtom<float> __EnchantEfficiencyCoef;
		private ComputedAtom<int> __TotalCrystalSlots;
		private ComputedAtom<bool> __HasPass;
		private ComputedAtom<bool> __MagicStatsUnlocked;
		private ComputedAtom<bool> __MagicSpecialStatUnlocked;
		private ComputedAtom<bool> __LifestealStatUnlocked;
		private ComputedAtom<TimeSpan> __TimeToNextEnchantStoneReset;
		private ComputedAtom<float> __BaseStatBonusPercent;
		public int ObjectiveGeodesRequiredForRespawn
		{
			get
			{
				return config.ObjectiveGeodesRequiredForRespawn;
			}
		}

		public CrystalConfig Config => this.config;
		public UpgradeModel Upgrades { get; private set; }

		public bool IsUnlocked
		{
			get
			{
				// Native ELF0x151eb74: original data-backed atom, not a computed progression guess.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlocked == null)
					__IsUnlocked = CodeGenAtom.CreatePooled(this, null, () => IsUnlocked, false);
				return __IsUnlocked.DirectEvaluate() ? Data.IsUnlocked.Value : __IsUnlocked.Value;
			}

			set
			{
				// Native ELF0x151ece0.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlocked != null && !__IsUnlocked.CompareAndInvalidate(value))
					return;
				Data.IsUnlocked.Value = value;
			}
		}

		public int ObjectiveGeodesProgress
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ObjectiveGeodesProgress == null)
					__ObjectiveGeodesProgress = CodeGenAtom.CreatePooled(this, null, () => ObjectiveGeodesProgress, false);
				return __ObjectiveGeodesProgress.DirectEvaluate() ? Data.ObjectiveGeodesProgress.Value : __ObjectiveGeodesProgress.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ObjectiveGeodesProgress != null && !__ObjectiveGeodesProgress.CompareAndInvalidate(value))
					return;
				Data.ObjectiveGeodesProgress.Value = value;
			}
		}

		public int ObjectiveKillCount
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ObjectiveKillCount == null)
					__ObjectiveKillCount = CodeGenAtom.CreatePooled(this, null, () => ObjectiveKillCount, false);
				return __ObjectiveKillCount.DirectEvaluate() ? Data.ObjectiveKillCount.Value : __ObjectiveKillCount.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ObjectiveKillCount != null && !__ObjectiveKillCount.CompareAndInvalidate(value))
					return;
				Data.ObjectiveKillCount.Value = value;
			}
		}

		public bool IsObjectiveWaitingForRespawn
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsObjectiveWaitingForRespawn == null)
					__IsObjectiveWaitingForRespawn = CodeGenAtom.CreatePooled(this, null, () => IsObjectiveWaitingForRespawn, false);
				return __IsObjectiveWaitingForRespawn.DirectEvaluate() ? Data.IsObjectiveWaitingForRespawn.Value : __IsObjectiveWaitingForRespawn.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsObjectiveWaitingForRespawn != null && !__IsObjectiveWaitingForRespawn.CompareAndInvalidate(value))
					return;
				Data.IsObjectiveWaitingForRespawn.Value = value;
			}
		}

		public bool IsWaitingForRespawn
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsWaitingForRespawn == null)
					__IsWaitingForRespawn = CodeGenAtom.CreatePooled(this, null, () => IsWaitingForRespawn, false);
				return __IsWaitingForRespawn.DirectEvaluate() ? IsObjectiveWaitingForRespawn && ObjectiveGeodesProgress < config.ObjectiveGeodesRequiredForRespawn : __IsWaitingForRespawn.Value;
			}
		}

		public bool IsObjectiveRespawnReady
		{
			get
			{
				return IsObjectiveWaitingForRespawn && ObjectiveGeodesProgress >= config.ObjectiveGeodesRequiredForRespawn;
			}
		}

		public bool NeedsObjectiveSpawnerActive
		{
			get
			{
				return IsWaitingForRespawn || IsObjectiveRespawnReady;
			}
		}

		public bool CanShowObjectiveBanner
		{
			get
			{
				return UnityEngine.Time.unscaledTime >= objectiveBannerShowAtUnscaledTime;
			}
		}

		public List<CrystalItemModel> Crystals
		{
			get
			{
				// Native ELF VA 0x151f5ec: observe repository Version through values, then snapshot.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Crystals == null)
					__Crystals = CodeGenAtom.CreatePooled(this, null, () => Crystals, false);
				return __Crystals.DirectEvaluate() ? values.ToList() : __Crystals.Value;
			}
		}

		public List<CrystalSlotModel> Slots { get; }

		public double GeodesCount
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__GeodesCount == null)
					__GeodesCount = CodeGenAtom.CreatePooled(this, null, () => GeodesCount, false);
				if (!__GeodesCount.DirectEvaluate())
					return __GeodesCount.Value;
				return currencyModel.Get("crystal.geodes");
			}
		}

		public double EnchantmentStonesCount
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EnchantmentStonesCount == null)
					__EnchantmentStonesCount = CodeGenAtom.CreatePooled(this, null, () => EnchantmentStonesCount, false);
				if (!__EnchantmentStonesCount.DirectEvaluate())
					return __EnchantmentStonesCount.Value;
				return currencyModel.Get("crystal.enchantment_stones");
			}
		}

		public double MagicDust
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MagicDust == null)
					__MagicDust = CodeGenAtom.CreatePooled(this, null, () => MagicDust, false);
				if (!__MagicDust.DirectEvaluate())
					return __MagicDust.Value;
				return currencyModel.Get("crystal.magic_dust");
			}
		}

		public MetaStat Blacksmithing => metaStatsModel.Get("crystal_blacksmithing"); // Native ELF0x151ff04.

		public MetaStat Enchantment
		{
			get
			{
				return metaStatsModel.Get("crystal_enchantment");
			}
		}

		public MetaStat EnchantEfficiency
		{
			get
			{
				return metaStatsModel.Get("crystal_enchant_efficiency");
			}
		}

		public MetaStat MaxGeodesStat
		{
			get
			{
				return metaStatsModel.Get("crystal_max_geodes");
			}
		}

		public MetaStat GeodeTimerStat
		{
			get
			{
				return metaStatsModel.Get("crystal_geode_timer");
			}
		}

		public MetaStat SellPriceBonusStat => metaStatsModel.Get("crystal_sell_price_bonus"); // Native ELF0x15200a8.

		public MetaStat CrystalsQualityStat
		{
			get
			{
				return metaStatsModel.Get("crystal_crystals_quality");
			}
		}

		public MetaStat MaxEnchantStonesStat
		{
			get
			{
				return metaStatsModel.Get("crystal_max_enchant_stones");
			}
		}

		public MetaStat BaseStatBonusStat
		{
			get
			{
				return metaStatsModel.Get("crystal_base_stat_bonus");
			}
		}

		public MetaStat TimerOnKillReduceStat
		{
			get
			{
				return metaStatsModel.Get("crystal_timer_on_kill_reduce");
			}
		}

		public MetaStat StatDrop
		{
			get
			{
				return metaStatsModel.Get("stat_drop");
			}
		}

		public MetaStat GuaranteeEnchantStat
		{
			get
			{
				return metaStatsModel.Get("crystal_guarantee_enchant");
			}
		}

		public int GuaranteeEnchantThreshold
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__GuaranteeEnchantThreshold == null)
					__GuaranteeEnchantThreshold = CodeGenAtom.CreatePooled(this, null, () => GuaranteeEnchantThreshold, false);
				if (!__GuaranteeEnchantThreshold.DirectEvaluate())
					return __GuaranteeEnchantThreshold.Value;
				var stat = GuaranteeEnchantStat;
				double value = stat != null ? stat.Value : 0.0;
				return double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
			}
		}

		public bool IsGuaranteeEnchantUnlocked
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsGuaranteeEnchantUnlocked == null)
					__IsGuaranteeEnchantUnlocked = CodeGenAtom.CreatePooled(this, null, () => IsGuaranteeEnchantUnlocked, false);
				if (!__IsGuaranteeEnchantUnlocked.DirectEvaluate())
					return __IsGuaranteeEnchantUnlocked.Value;
				return GuaranteeEnchantThreshold > 0;
			}
		}

		public int GuaranteeEnchantProgress
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__GuaranteeEnchantProgress == null)
					__GuaranteeEnchantProgress = CodeGenAtom.CreatePooled(this, null, () => GuaranteeEnchantProgress, false);
				if (!__GuaranteeEnchantProgress.DirectEvaluate())
					return __GuaranteeEnchantProgress.Value;
				return Data.GuaranteeEnchantProgress.Value;
			}
		}

		public bool IsGuaranteeEnchantReady
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsGuaranteeEnchantReady == null)
					__IsGuaranteeEnchantReady = CodeGenAtom.CreatePooled(this, null, () => IsGuaranteeEnchantReady, false);
				if (!__IsGuaranteeEnchantReady.DirectEvaluate())
					return __IsGuaranteeEnchantReady.Value;
				return IsGuaranteeEnchantUnlocked && Data.GuaranteeEnchantProgress.Value >= GuaranteeEnchantThreshold;
			}
		}

		public int MaxGeodes
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxGeodes == null)
					__MaxGeodes = CodeGenAtom.CreatePooled(this, null, () => MaxGeodes, false);
				if (!__MaxGeodes.DirectEvaluate())
					return __MaxGeodes.Value;
				var stat = MaxGeodesStat;
				double value = stat != null ? stat.Value : 0.0;
				int extra = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
				return unchecked(config.BaseMaxGeodes + extra);
			}
		}

		public int GeodeTimerMinutes
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__GeodeTimerMinutes == null)
					__GeodeTimerMinutes = CodeGenAtom.CreatePooled(this, null, () => GeodeTimerMinutes, false);
				if (!__GeodeTimerMinutes.DirectEvaluate())
					return __GeodeTimerMinutes.Value;
				var stat = GeodeTimerStat;
				double value = stat != null ? stat.Value : 0.0;
				int reduction = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
				return Math.Max(config.MinGeodeTimerMinutes, unchecked(config.BaseGeodeTimerMinutes - reduction));
			}
		}

		public float SecondsUntilNextGeode
		{
			get
			{
				int minutes = GeodeTimerMinutes;
				if (minutes < 1 || Data.LastGeodeTimestamp.Value <= 0)
					return 0f;
				double elapsed = (timeService.Now.ToDateTime() - new DateTime(Data.LastGeodeTimestamp.Value)).TotalMinutes;
				double remaining = ((double)minutes - elapsed % (double)minutes) * 60.0;
				return (float)(remaining > 0.0 ? remaining : 0.0);
			}
		}

		public int MaxEnchantmentStones
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxEnchantmentStones == null)
					__MaxEnchantmentStones = CodeGenAtom.CreatePooled(this, null, () => MaxEnchantmentStones, false);
				if (!__MaxEnchantmentStones.DirectEvaluate())
					return __MaxEnchantmentStones.Value;
				var stat = MaxEnchantStonesStat;
				double value = stat != null ? stat.Value : 0.0;
				int extra = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
				return unchecked(config.BaseMaxEnchantmentStones + extra);
			}
		}

		public float SellPriceBonusPercent
		{
			get
			{
				// Native ELF0x1520e60 and float32 cast-before-multiply semantics.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SellPriceBonusPercent == null)
					__SellPriceBonusPercent = CodeGenAtom.CreatePooled(this, null, () => SellPriceBonusPercent, false);
				return __SellPriceBonusPercent.DirectEvaluate() ? Math.Min(SellPriceBonusStat == null ? 0f : (float)SellPriceBonusStat.Value * 100f, config.SellPriceMaxBonusPercent) : __SellPriceBonusPercent.Value;
			}
		}

		public float CrystalsQualityBonus
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CrystalsQualityBonus == null)
					__CrystalsQualityBonus = CodeGenAtom.CreatePooled(this, null, () => CrystalsQualityBonus, false);
				if (!__CrystalsQualityBonus.DirectEvaluate())
					return __CrystalsQualityBonus.Value;
				var stat = CrystalsQualityStat;
				return stat != null ? (float)stat.Value * 100f : 0f;
			}
		}

		public float EnchantEfficiencyCoef
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EnchantEfficiencyCoef == null)
					__EnchantEfficiencyCoef = CodeGenAtom.CreatePooled(this, null, () => EnchantEfficiencyCoef, false);
				if (!__EnchantEfficiencyCoef.DirectEvaluate())
					return __EnchantEfficiencyCoef.Value;
				var stat = EnchantEfficiency;
				double value = stat != null ? stat.Value : (double)config.BaseEnchEfficiencyCoef;
				return (float)Math.Min(value, (double)config.MaxEnchEfficiencyCoef);
			}
		}

		public int TotalCrystalSlots
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TotalCrystalSlots == null)
					__TotalCrystalSlots = CodeGenAtom.CreatePooled(this, null, () => TotalCrystalSlots, false);
				return __TotalCrystalSlots.DirectEvaluate() ? Slots.Count(slot => slot.IsUnlocked) : __TotalCrystalSlots.Value;
			}
		}

		public bool HasPass
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasPass == null)
					__HasPass = CodeGenAtom.CreatePooled(this, null, () => HasPass, false);
				if (!__HasPass.DirectEvaluate())
					return __HasPass.Value;
				return userModel.CrystalPassPurchased.Value;
			}
		}

		public bool MagicStatsUnlocked
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MagicStatsUnlocked == null)
					__MagicStatsUnlocked = CodeGenAtom.CreatePooled(this, null, () => MagicStatsUnlocked, false);
				if (!__MagicStatsUnlocked.DirectEvaluate())
					return __MagicStatsUnlocked.Value;
				return amuletModel.Level > 6;
			}
		}

		public bool MagicSpecialStatUnlocked
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MagicSpecialStatUnlocked == null)
					__MagicSpecialStatUnlocked = CodeGenAtom.CreatePooled(this, null, () => MagicSpecialStatUnlocked, false);
				if (!__MagicSpecialStatUnlocked.DirectEvaluate())
					return __MagicSpecialStatUnlocked.Value;
				return amuletModel.Level > 7;
			}
		}

		public bool LifestealStatUnlocked
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__LifestealStatUnlocked == null)
					__LifestealStatUnlocked = CodeGenAtom.CreatePooled(this, null, () => LifestealStatUnlocked, false);
				if (!__LifestealStatUnlocked.DirectEvaluate())
					return __LifestealStatUnlocked.Value;
				return false;
			}
		}

		public TimeSpan TimeToNextEnchantStoneReset
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TimeToNextEnchantStoneReset == null)
					__TimeToNextEnchantStoneReset = CodeGenAtom.CreatePooled(this, null, () => TimeToNextEnchantStoneReset, false);
				if (!__TimeToNextEnchantStoneReset.DirectEvaluate())
					return __TimeToNextEnchantStoneReset.Value;
				DateTime now = timeService.Now.ToDateTime();
				return now.Date.AddDays(1.0) - now;
			}
		}

		public float BaseStatBonusPercent
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__BaseStatBonusPercent == null)
					__BaseStatBonusPercent = CodeGenAtom.CreatePooled(this, null, () => BaseStatBonusPercent, false);
				if (!__BaseStatBonusPercent.DirectEvaluate())
					return __BaseStatBonusPercent.Value;
				var stat = BaseStatBonusStat;
				float value = stat != null ? (float)stat.Value * 100f : 0f;
				return value + CrystalsQualityBonus;
			}
		}

		public CrystalSlotModel GetSlot(string slotKey)
		{
			foreach (var slot in Slots)
				if (slot.Key == slotKey)
					return slot;
			return null;
		}

		public CrystalItemModel GetEquippedCrystal(string slotKey)
		{
			foreach (var crystal in Crystals)
				if (crystal.EquippedSlotKey == slotKey)
					return crystal;
			return null;
		}

		public float GetEnchantEfficiencyCoefForCrystal(CrystalItemModel crystal)
		{
			var stat = EnchantEfficiency;
			double value = stat != null ? stat.Value : (double)config.BaseEnchEfficiencyCoef;
			if (crystal != null && crystal.LegendaryAbility == LegendaryAbilityType.EnhancedEnchant)
				value += CrystalBalance.GetLegendaryAbilityValue(config, LegendaryAbilityType.EnhancedEnchant, crystal.EnchantLevel) / 100.0;
			return (float)Math.Min(value, (double)config.MaxEnchEfficiencyCoef);
		}

		public CrystalModel(Lifetime lifetime) : base(lifetime)
		{
			// Native ELF VA 0x1521df0.
			appliedModifiersByCrystalId = new Dictionary<int, List<(StatBase stat, StatModifier mod)>>();
			random = new Random();
			Slots = new List<CrystalSlotModel>();
		}

		public void Populate()
		{
			foreach (var data in Data.Crystals)
				Add(new CrystalItemModel(Lifetime, data));
			PopulateSlots();
			GrantStartingGeodesIfNeeded();
			EnsureGeodeTimerStarted();
			PopulateUpgrades();
			foreach (var slot in Slots)
			{
				var captured = slot;
				Atom.Reaction(Lifetime, () => IsSlotConditionMet(captured), _ => SyncSlotLockState(captured));
			}

			foreach (var slot in Slots)
				SyncSlotLockState(slot);
			SyncObjectiveWaitingForRespawnState();
		}

		private void SyncSlotLockState(CrystalSlotModel slot)
		{
			bool unlocked = IsSlotConditionMet(slot);
			if (!unlocked)
			{
				if (slot.IsUnlocked)
				{
					UnequipCrystalFromSlot(slot.Key);
					slot.IsUnlocked = false;
				}
			}
			else if (!slot.IsUnlocked)
				slot.IsUnlocked = true;
		}

		private void UnequipCrystalFromSlot(string slotKey)
		{
			var crystal = GetEquippedCrystal(slotKey);
			if (crystal != null)
			{
				RemoveCrystalModifiers(crystal);
				crystal.Unequip();
			}
		}

		private void PopulateUpgrades()
		{
			Upgrades = new UpgradeModel(Lifetime, (ModifierSource)9);
			Injector.InjectInto(Upgrades);
			Upgrades.Setup(CrystalUpgradeDefsBuilder.Build(config), Data.UpgradeLevels, metaStatsModel.All);
		}

		private void PopulateSlots()
		{
			Slots.Clear();
			var saved = new Dictionary<string, CrystalSlotDataUd>();
			foreach (var ud in Data.Slots)
				if (!string.IsNullOrEmpty(ud.SlotKey.Value))
					saved[ud.SlotKey.Value] = ud;
			foreach (var def in config.Slots)
			{
				var slot = new CrystalSlotModel(Lifetime, def.Key, def.UnlockedByDefault, def.UnlockSource, def.UnlockUpgradeKey);
				if (!saved.TryGetValue(def.Key, out var ud))
				{
					ud = Data.Slots.Create();
					ud.SlotKey.Value = def.Key;
					ud.IsUnlocked.Value = def.UnlockedByDefault;
				}

				slot.BindToUserData(ud);
				Slots.Add(slot);
			}
		}

		private bool IsSlotConditionMet(CrystalSlotModel slot)
		{
			switch ((int)slot.UnlockSource)
			{
				case 0:
					return true;
				case 1:
				case 4:
					return metaStatsModel.Get("feature_unlock").GetValue(slot.Key) > 0.0;
				case 2:
					return HasPass;
				case 3:
					return IsVipSlotUnlocked(slot.Key);
				default:
					return false;
			}
		}

		private bool IsVipSlotUnlocked(string slotKey)
		{
			foreach (var feature in vipModel.GetAllFeatures<Codebase.VIP.Features.VipCrystalSlotsFeature>("crystal_slots"))
				if (feature.SlotKey == slotKey)
					return true;
			return false;
		}

		private void GrantStartingGeodesIfNeeded()
		{
			if (Data.LastGeodeTimestamp.Value == 0 && GeodesCount <= 0.0)
				currencyModel.Set("crystal.geodes", (double)MaxGeodes);
		}

		private void EnsureGeodeTimerStarted()
		{
			if (Data.LastGeodeTimestamp.Value <= 0)
				Data.LastGeodeTimestamp.Value = timeService.Now.ToDateTime().Ticks;
		}

		public void AddMagicDust(double amount)
		{
			currencyModel.Add("crystal.magic_dust", amount);
		}

		public bool TrySpendMagicDust(double amount)
		{
			return currencyModel.TrySpend("crystal.magic_dust", amount);
		}

		public void ReduceGeodeTimer(double seconds)
		{
			if (!(seconds > 0.0))
				return;
			long last = Data.LastGeodeTimestamp.Value;
			if (last <= 0)
				return;
			double value = seconds * 10000000.0;
			long ticks = double.IsPositiveInfinity(value) ? long.MinValue : value >= 9223372036854775808.0 ? long.MaxValue : value <= -9223372036854775808.0 ? long.MinValue : double.IsNaN(value) ? 0L : (long)value;
			Data.LastGeodeTimestamp.Value = unchecked(last - ticks);
			ProcessPendingGeodes();
		}

		public void ProcessPendingGeodes()
		{
			long last = Data.LastGeodeTimestamp.Value;
			if (last <= 0)
				return;
			TimeSpan elapsed = timeService.Now.ToDateTime() - new DateTime(last);
			int minutes = GeodeTimerMinutes;
			if (minutes <= 0)
				return;
			double value = elapsed.TotalMinutes / (double)minutes;
			int amount = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
			if (amount > 0)
			{
				AddGeodes(amount);
				Data.LastGeodeTimestamp.Value = unchecked(last + (long)unchecked(amount * minutes) * 600000000L);
			}
		}

		public void AddGeodes(int amount)
		{
			if (amount < 1)
				return;
			currencyModel.Set("crystal.geodes", Math.Min(GeodesCount + (double)amount, (double)MaxGeodes));
		}

		public bool TrySpendGeode()
		{
			return currencyModel.TrySpend("crystal.geodes", 1.0);
		}

		public bool ShouldForceLegendaryGeode()
		{
			return Data.GeodesOpenedCount.Value == 19;
		}

		public bool ShouldForceEpicGeode()
		{
			return Data.GeodesOpenedCount.Value == 2;
		}

		public void RegisterGeodeOpened()
		{
			RegisterObjectiveGeodeIfWaiting();
			if (Data.GeodesOpenedCount.Value < 20)
				Data.GeodesOpenedCount.Value = unchecked(Data.GeodesOpenedCount.Value + 1);
		}

		public void OnObjectiveDestroyed()
		{
			int previous = ObjectiveKillCount;
			using (Atom.NoWatch)
			{
				ObjectiveGeodesProgress = 0;
				ObjectiveKillCount = unchecked(ObjectiveKillCount + 1);
				IsObjectiveWaitingForRespawn = true;
			}

			analytics.Send(new Codebase.Analytics.Events.CrystalsTormentorVictoryEvent(unchecked(previous + 1)));
			objectiveBannerShowAtUnscaledTime = UnityEngine.Time.unscaledTime + 1.5f;
		}

		public void GrantObjectiveKillReward()
		{
			Blacksmithing.AddModifier(new StatModifier { Name = "crystal_objective_kill", ModifierType = (ModifierType)0, Source = (ModifierSource)17, Value = 3f });
			statRewardContainer.Create(string.Format("<color=green>+{0}<sprite name=atlas_97>", 3), false);
		}

		public void ResetObjectiveBannerVisibility()
		{
			objectiveBannerShowAtUnscaledTime = 0f;
		}

		public void SetupObjectiveUnit(CrystalUnit unit)
		{
			unit.BlacksmithKillReward = IsUnlocked ? 3 : 0;
			var stats = CrystalObjectiveBalance.GetUnitStats(config, simulationConfig, GetObjectiveSpawner(), ObjectiveKillCount);
			CrystalObjectiveBalance.ApplyToUnit(unit, Lifetime, config, stats);
		}

		public void TryCompleteRespawn()
		{
			if (ObjectiveGeodesProgress < config.ObjectiveGeodesRequiredForRespawn || IsObjectiveCrystalAlive())
				return;
			var spawner = GetObjectiveSpawner();
			if (spawner == null || spawner.AnyoneAlive || !spawner.RespawnCrystalObjective())
				return;
			using (Atom.NoWatch)
			{
				ObjectiveGeodesProgress = 0;
				IsObjectiveWaitingForRespawn = false;
			}

			GameContext.Get<CrystalUnitScene>().ResetAliveState();
			pendingCrystalRespawnedPopup = true;
			if (Codebase.UI.Crystals.AmuletCrystalsView.IsOpen)
				return;
			TryShowCrystalRespawnedPopup();
		}

		public void TryShowCrystalRespawnedPopup()
		{
			if (!pendingCrystalRespawnedPopup)
				return;
			pendingCrystalRespawnedPopup = false;
			GameContext.Get<Codebase.Core.UI.UINavigator>().Open<Codebase.UI.Crystals.CrystalRespawnedView>("Assets/Prefabs/UI/Amulet/Crystals/CrystalRespawnedView.prefab", true);
		}

		public int GetObjectiveGeodesRemaining()
		{
			return Math.Max(0, unchecked(config.ObjectiveGeodesRequiredForRespawn - ObjectiveGeodesProgress));
		}

		public bool IsObjectiveCrystalAlive()
		{
			foreach (var enemy in GameContext.Get<Codebase.Enemy.EnemiesModel>().Enemies)
				if (!ReferenceEquals(enemy, null) && enemy is CrystalUnit && !enemy.IsDead)
					return true;
			return false;
		}

		public EnemySpawner GetObjectiveSpawner()
		{
			foreach (var spawner in GameContext.Get<Codebase.Enemy.EnemiesModel>().Spawners)
				if (spawner.Config.Prefab is CrystalUnit)
					return spawner;
			return null;
		}

		private void RegisterObjectiveGeodeIfWaiting()
		{
			if (!IsObjectiveWaitingForRespawn)
				return;
			if (ObjectiveGeodesProgress < config.ObjectiveGeodesRequiredForRespawn)
				ObjectiveGeodesProgress = unchecked(ObjectiveGeodesProgress + 1);
			TryCompleteRespawn();
		}

		private void SyncObjectiveWaitingForRespawnState()
		{
			if (!IsObjectiveWaitingForRespawn && ObjectiveGeodesProgress > 0 && ObjectiveGeodesProgress < config.ObjectiveGeodesRequiredForRespawn)
				IsObjectiveWaitingForRespawn = true;
		}

		public void AddEnchantmentStones(int amount)
		{
			currencyModel.Add("crystal.enchantment_stones", (double)amount);
		}

		public bool TrySpendEnchantmentStone()
		{
			return currencyModel.TrySpend("crystal.enchantment_stones", 1.0);
		}

		public void RegisterEnchant()
		{
			if (IsGuaranteeEnchantUnlocked)
				Data.GuaranteeEnchantProgress.Value = unchecked(Data.GuaranteeEnchantProgress.Value + 1);
		}

		public void ConsumeGuaranteeEnchant()
		{
			Data.GuaranteeEnchantProgress.Value = Math.Max(0, unchecked(Data.GuaranteeEnchantProgress.Value - GuaranteeEnchantThreshold));
		}

		public void ResetDailyEnchantmentStones()
		{
			long today = timeService.Now.ToDateTime().Date.Ticks;
			if (today <= Data.LastStonesResetDay.Value)
				return;
			int maximum = MaxEnchantmentStones;
			int multiplier = cheatsModel.DropMultiplier;
			double value = EnchantmentStonesCount;
			int current = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
			currencyModel.Set("crystal.enchantment_stones", (double)Math.Max(unchecked(multiplier * maximum), current));
			Data.LastStonesResetDay.Value = today;
		}

		public CrystalItemUd CreateCrystalUd()
		{
			using (Atom.NoWatch)
			{
				var created = Data.Crystals.Create();
				int id = Data.NextCrystalId.Value;
				Data.NextCrystalId.Value = unchecked(id + 1);
				created.Id.Value = id;
				return created;
			}
		}

		public double RescaleBaseStatForEnchantLevel(CrystalItemModel crystal, StatType statType, double currentValue, int fromLevel, int toLevel)
		{
			return CrystalBalance.RescaleBaseStatForEnchantLevel(config, simulationConfig.SimulationData, currentValue, fromLevel, toLevel, crystal.Level, crystal.Rarity, statType, GetEnchantEfficiencyCoefForCrystal(crystal));
		}

		public CrystalItemModel RegisterCrystal(CrystalItemUd crystalUd)
		{
			var model = new CrystalItemModel(Lifetime, crystalUd);
			Add(model);
			return model;
		}

		public void ApplyEnchantLevel(CrystalItemModel crystal)
		{
			if (IsAtMaxEnchantLevel(crystal))
				return;
			int previous = crystal.EnchantLevel;
			int next = unchecked(previous + 1);
			crystal.SetEnchantLevel(next);
			if (!crystal.HasSpecialStat)
				crystal.SetSpecialStat(RollSpecialStat(), 0.0);
			crystal.SetSpecialStatValue((double)CrystalBalance.GetSpecialStatValue(config, crystal.SpecialStatType, next, GetEnchantEfficiencyCoefForCrystal(crystal)));
			crystal.SetBaseStatValue(RescaleBaseStatForEnchantLevel(crystal, crystal.BaseStatType, crystal.BaseStatValue, previous, next));
			if (crystal.HasSecondaryStat)
				crystal.SetSecondaryStatValue(RescaleBaseStatForEnchantLevel(crystal, crystal.SecondaryStatType, crystal.SecondaryStatValue, previous, next));
			if (crystal.IsEquipped)
				ApplyCrystalModifiers(crystal);
		}

		public StatType RollSpecialStat()
		{
			var available = new List<SpecialStatDef>();
			foreach (var def in config.SpecialStats)
			{
				if (def.LockedByDefault && def.StatType == (StatType)32 && !LifestealStatUnlocked)
					continue;
				if (def.LockedByDefault && def.StatType == (StatType)30 && !MagicSpecialStatUnlocked)
					continue;
				available.Add(def);
			}

			return available.Count == 0 ? (StatType)23 : available[random.Next(available.Count)].StatType;
		}

		public void RemoveCrystal(CrystalItemModel crystal)
		{
			RemoveCrystalModifiers(crystal);
			int index = values.IndexOf(crystal);
			if (index < 0)
				return;
			Data.Crystals.RemoveAt(index);
			Remove(crystal);
		}

		public bool IsAtMaxEnchantLevel(CrystalItemModel crystal)
		{
			return crystal != null && crystal.EnchantLevel >= GetMaxEnchantLevel(crystal);
		}

		public int GetNextEnchantLevel(CrystalItemModel crystal)
		{
			return crystal != null ? Math.Min(unchecked(crystal.EnchantLevel + 1), GetMaxEnchantLevel(crystal)) : 0;
		}

		public int GetMaxEnchantLevel(CrystalItemModel crystal)
		{
			return crystal != null ? GetMaxEnchantLevel(crystal.Rarity) : 0;
		}

		public int GetMaxEnchantLevel(CrystalRarity rarity)
		{
			return Math.Min(unchecked((int)rarity + 1), config.MaxEnchantLevel);
		}

		public bool CanEnchant(CrystalItemModel crystal)
		{
			return crystal != null && !IsAtMaxEnchantLevel(crystal) && EnchantmentStonesCount > 0.0;
		}

		public bool IsEnchantUnlocked(CrystalItemModel crystal)
		{
			if (crystal != null)
			{
				var ignored = crystal.Rarity;
				return true;
			}

			return false;
		}

		public bool IsEnchantUnlocked(CrystalRarity rarity)
		{
			return true;
		}

		public double CalculateSellPrice(CrystalItemModel crystal)
		{
			return CalculateSellPriceForLevel(crystal, crystal.EnchantLevel);
		}

		public double CalculatePreviewSellPrice(CrystalItemModel crystal)
		{
			return CalculateSellPriceForLevel(crystal, GetNextEnchantLevel(crystal));
		}

		public double CalculateExpectedDustPerCrystal()
		{
			// Native ELF0x1527694; published rarity floats are promoted individually before the original double accumulation.
			double value = Blacksmithing.Value;
			int blacksmithing = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
			var chances = CrystalBalance.GetRarityChances(config, blacksmithing);
			double expected = 0.0;
			foreach (var chance in chances)
				expected += CrystalBalance.CalculateSellPrice(config, chance.Key, blacksmithing, 0, LegendaryAbilityType.None, SellPriceBonusPercent, 0.0) * (double)chance.Value;
			return expected;
		}

		public double CalculateMagicDustForRpm(float rpm)
		{
			return Math.Round(CalculateExpectedDustPerCrystal() * (double)rpm);
		}

		public double CalculatePreviewBaseStatValue(CrystalItemModel crystal)
		{
			return IsAtMaxEnchantLevel(crystal) ? crystal.BaseStatValue : RescaleBaseStatForEnchantLevel(crystal, crystal.BaseStatType, crystal.BaseStatValue, crystal.EnchantLevel, unchecked(crystal.EnchantLevel + 1));
		}

		public double CalculatePreviewSecondaryStatValue(CrystalItemModel crystal)
		{
			return IsAtMaxEnchantLevel(crystal) ? crystal.SecondaryStatValue : RescaleBaseStatForEnchantLevel(crystal, crystal.SecondaryStatType, crystal.SecondaryStatValue, crystal.EnchantLevel, unchecked(crystal.EnchantLevel + 1));
		}

		public float GetEnchantSuccessChance(CrystalItemModel crystal)
		{
			if (crystal == null || IsAtMaxEnchantLevel(crystal))
				return 0f;
			return CrystalBalance.GetEnchantSuccessChance(config, crystal.Rarity, Enchantment != null ? Enchantment.Value : 0.0);
		}

		public int GetEnchantExtraAttempts(int enchantLevel)
		{
			if (enchantLevel < 0)
				return 0;
			var stat = metaStatsModel.Get("crystal_enchant_extra_attempts");
			if (stat == null)
				return 0;
			double value = stat.GetValue(enchantLevel);
			return double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
		}

		public float GetSpecialStatValue(CrystalItemModel crystal, StatType type, int enchLevel)
		{
			return crystal.EnchantLevel == enchLevel ? (float)crystal.SpecialStatValue : CrystalBalance.GetSpecialStatValue(config, type, enchLevel, GetEnchantEfficiencyCoefForCrystal(crystal));
		}

		public float CalculateSpecialStatValue(CrystalItemModel crystal, StatType type, int enchLevel)
		{
			return CrystalBalance.GetSpecialStatValue(config, type, enchLevel, GetEnchantEfficiencyCoefForCrystal(crystal));
		}

		public ModifierType GetSpecialStatModifierType(StatType type)
		{
			return CrystalBalance.GetSpecialStatModifierType(config, type);
		}

		public double CalculateBaseStatValue(int blacksmithing, CrystalRarity rarity, StatType statType)
		{
			return CrystalBalance.CalculateBaseStatValue(config, simulationConfig.SimulationData, blacksmithing, rarity, statType, BaseStatBonusPercent);
		}

		public void ApplyAllEquippedToUnit()
		{
			foreach (int id in appliedModifiersByCrystalId.Keys.ToList())
				RemoveCrystalModifiersById(id);
			foreach (var crystal in Crystals)
				if (crystal.IsEquipped)
					ApplyCrystalModifiers(crystal);
		}

		public void ApplyCrystalModifiers(CrystalItemModel crystal)
		{
			RemoveCrystalModifiers(crystal);
			var unit = playerModel != null ? playerModel.Unit : null;
			if (unit == null)
				return;
			var modifiers = BuildCrystalModifiers(crystal);
			var applied = new List<(StatBase stat, StatModifier mod)>();
			foreach (var modifier in modifiers)
			{
				var stat = unit.Stats.FirstOrDefault(candidate => candidate.Type == modifier.StatType);
				if (stat == null)
					continue;
				RemoveModifiersByName(stat, modifier.Name);
				stat.AddModifier(modifier);
				applied.Add((stat, modifier));
			}

			foreach (var pair in BuildCrystalMetaStatModifiers(crystal))
			{
				RemoveModifiersByName(pair.Item1, pair.Item2.Name);
				pair.Item1.AddModifier(pair.Item2);
				applied.Add((pair.Item1, pair.Item2));
			}

			appliedModifiersByCrystalId[crystal.Id] = applied;
		}

		public void RemoveCrystalModifiers(CrystalItemModel crystal)
		{
			RemoveCrystalModifiersById(crystal.Id);
			PurgeOrphanedModifiersForCrystal(crystal);
		}

		private List<StatModifier> BuildCrystalModifiers(CrystalItemModel crystal)
		{
			var result = new List<StatModifier>();
			result.Add(new StatModifier { Name = string.Format("crystal_{0}_base", crystal.Id), StatType = crystal.BaseStatType, ModifierType = (ModifierType)0, Source = (ModifierSource)8, Value = (float)crystal.BaseStatValue });
			if (crystal.HasSecondaryStat)
				result.Add(new StatModifier { Name = string.Format("crystal_{0}_secondary", crystal.Id), StatType = crystal.SecondaryStatType, ModifierType = (ModifierType)0, Source = (ModifierSource)8, Value = (float)crystal.SecondaryStatValue });
			if (crystal.HasSpecialStat)
			{
				var modifierType = CrystalBalance.GetSpecialStatModifierType(config, crystal.SpecialStatType);
				result.Add(new StatModifier { Name = string.Format("crystal_{0}_special", crystal.Id), StatType = crystal.SpecialStatType, ModifierType = modifierType, Source = (ModifierSource)8, Value = (float)crystal.SpecialStatValue / 100f });
			}

			if (crystal.LegendaryAbility == LegendaryAbilityType.SuperCrit)
				result.Add(new StatModifier { Name = string.Format("crystal_{0}_supercrit", crystal.Id), StatType = (StatType)29, ModifierType = (ModifierType)6, Source = (ModifierSource)8, Value = (float)CrystalBalance.GetLegendaryAbilityValue(config, crystal) / 100f });
			if (crystal.LegendaryAbility == LegendaryAbilityType.SuperCritChance)
				result.Add(new StatModifier { Name = string.Format("crystal_{0}_supercrit_chance", crystal.Id), StatType = (StatType)28, ModifierType = (ModifierType)6, Source = (ModifierSource)8, Value = (float)CrystalBalance.GetLegendaryAbilityValue(config, crystal) / 100f });
			return result;
		}

		private List<(MetaStat, StatModifier)> BuildCrystalMetaStatModifiers(CrystalItemModel crystal)
		{
			var result = new List<(MetaStat, StatModifier)>();
			string key, name;
			ModifierType type = (ModifierType)0;
			switch (crystal.LegendaryAbility)
			{
				case LegendaryAbilityType.TowerDamageBonus:
					key = "tower_damage";
					name = "crystal_{0}_tower_damage";
					break;
				case LegendaryAbilityType.TowerDamageReduction:
					key = "tower_damage_reduction";
					name = "crystal_{0}_tower_damage_reduction";
					break;
				case LegendaryAbilityType.StatDropBonus:
					key = "stat_drop";
					name = "crystal_{0}_stat_drop";
					type = (ModifierType)1;
					break;
				case LegendaryAbilityType.EconomyProcChancePerEnchant:
					key = "economy_item_abilities_chance";
					name = "crystal_{0}_economy_proc";
					break;
				default:
					return result;
			}

			var stat = metaStatsModel.Get(key);
			if (stat == null)
				return result;
			result.Add((stat, new StatModifier { Name = string.Format(name, crystal.Id), ModifierType = type, Source = (ModifierSource)8, Value = (float)CrystalBalance.GetLegendaryAbilityValue(config, crystal) / 100f }));
			return result;
		}

		private double CalculateSellPriceForLevel(CrystalItemModel crystal, int enchantLevel)
		{
			double legendaryBonus = CrystalBalance.GetLegendaryAbilityValue(config, LegendaryAbilityType.SellPriceBonus, enchantLevel);
			return CrystalBalance.CalculateSellPrice(config, crystal.Rarity, crystal.Level, enchantLevel, crystal.LegendaryAbility, SellPriceBonusPercent, legendaryBonus);
		}

		private void RemoveCrystalModifiersById(int crystalId)
		{
			if (!appliedModifiersByCrystalId.TryGetValue(crystalId, out var applied))
				return;
			foreach (var pair in applied)
			{
				pair.stat.RemoveModifier(pair.mod);
				RemoveModifiersByName(pair.stat, pair.mod.Name);
			}

			appliedModifiersByCrystalId.Remove(crystalId);
		}

		private void PurgeOrphanedModifiersForCrystal(CrystalItemModel crystal)
		{
			string prefix = string.Format("crystal_{0}_", crystal.Id);
			var unit = playerModel != null ? playerModel.Unit : null;
			if (unit != null)
				foreach (var stat in unit.Stats)
					PurgeOrphanedModifiers(stat, prefix);
			foreach (var stat in metaStatsModel.All.Values)
				PurgeOrphanedModifiers(stat, prefix);
		}

		private void PurgeOrphanedModifiers(StatBase stat, string prefix)
		{
			foreach (var modifier in stat.Modifiers.Where(modifier => modifier.Source == (ModifierSource)8 && modifier.Name != null && modifier.Name.StartsWith(prefix)).ToList())
				stat.RemoveModifier(modifier);
		}

		private void RemoveModifiersByName(StatBase stat, string name)
		{
			if (string.IsNullOrEmpty(name))
				return;
			foreach (var modifier in stat.Modifiers.Where(modifier => modifier.Name == name).ToList())
				stat.RemoveModifier(modifier);
		}

		private int GetGraphChaoticSoulsSpent()
		{
			return 0;
		}
	}
}
