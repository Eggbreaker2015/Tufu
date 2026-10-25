using UnityEngine;
using Codebase.VIP;
using Codebase.Simulation.Systems;
using Codebase.Core;
using System;
using System.Collections.Generic;
using Codebase.ABTests;
using Codebase.Unit;

namespace Codebase.Integrations.Ads
{
	public class GemsHudOfferModel
	{
		private static readonly GemsHudOfferData Data = new GemsHudOfferData(); // Native cctor14c989c.

		private static readonly StatType[] OfferableStats = new StatType[] { (StatType)1, (StatType)2, (StatType)22, (StatType)8, (StatType)6, (StatType)12, (StatType)16, (StatType)14, (StatType)20 }; // Original metadata blob SHA C3D653F8...

		private const float InitDelay = 1f;

		private float timer;

		private bool isShowPhase;

		private bool initialized;

		private float initTimer;

		public bool Paused { get; set; }

		public bool IsVisible => isShowPhase && CurrentOffer != null; // Native14c8cd4.

		public GemsHudOffer CurrentOffer { get; private set; }

		public float TimerNormalized { get {
            // Native14c8cfc ordered comparisons: NaN ratio returns0, unlike generic Mathf.Clamp01.
            if (!isShowPhase || !(Data.showDuration > 0f)) return 0f;
            float ratio = timer / Data.showDuration; if (!(ratio >= 0f)) return 0f; return ratio <= 1f ? ratio : 1f;
        } }

		public float TimerSeconds => isShowPhase ? timer : 0f; // Native14c8d98.

		public void Update(float deltaTime)
		{
            // Native ELF0x14c8b28.
            if (!GameContext.Get<ABTestService>().IsGemsHudOfferEnabled()) {
                CurrentOffer = null; isShowPhase = false; timer = 0f; initialized = false; initTimer = 0f; return;
            }
            if (Paused) return;
            if (!initialized) {
                initTimer += deltaTime; if (initTimer < InitDelay) return; initialized = true; isShowPhase = true;
            } else {
                timer -= deltaTime; if (timer > 0f) return;
                if (isShowPhase) { isShowPhase = false; timer = Data.hideDuration; CurrentOffer = null; return; }
                isShowPhase = true;
            }
            timer = Data.showDuration; GenerateOffer();
        }

		public void Hide()
		{
            // Native ELF0x14c9430.
            isShowPhase = false; timer = Data.hideDuration; CurrentOffer = null;
        }

		public void GenerateOffer()
		{
            // Native ELF0x14c8ff4.
            var data = Data; int experience = GameContext.Get<VipModel>().Experience; var eligible = GetEligibleStats();
            if (eligible.Count == 0) { CurrentOffer = null; return; }
            StatType type = eligible[UnityEngine.Random.Range(0, eligible.Count)]; double rpm = SimulationSystem.Instance.RPMCalculatedValue[type];
            GemsHudOfferTier tier = RollOfferTier(data); double multiplier = GetCostMultiplier(tier, data);
            double amount = multiplier * Math.Pow((double)experience * 10.0 + 1000.0, 0.9) * ((double)UnityEngine.Random.value * 0.2 + 0.9);
            int cost = NativeInt(Math.Max(1.0, Math.Round(amount))); double value = CalculateStatValue(cost, rpm, data); double baseRate = rpm * data.baseGemValue;
            int baseCost = baseRate > 0.0 ? NativeInt(Math.Max(1.0, Math.Round(value / baseRate))) : cost;
            CurrentOffer = new GemsHudOffer { StatType = type, Cost = cost, BaseCost = baseCost, Value = value, Tier = tier, Rpm = rpm };
        }

		private static List<StatType> GetEligibleStats()
		{
            // Native ELF0x14c94ac.
            var result = new List<StatType>(); var rpm = SimulationSystem.Instance.RPMCalculatedValue;
            foreach (StatType type in OfferableStats) if (rpm.TryGetValue(type, out double value) && value > 0.0) result.Add(type); return result;
        }

		private static GemsHudOfferTier RollOfferTier(GemsHudOfferData data)
		{
            // Native ELF0x14c967c.
            float roll = UnityEngine.Random.value * (data.tierWeightSmall + data.tierWeightMedium + data.tierWeightLarge);
            if (!(data.tierWeightLarge <= roll)) return GemsHudOfferTier.Large;
            return roll < data.tierWeightLarge + data.tierWeightMedium ? GemsHudOfferTier.Medium : GemsHudOfferTier.Small;
        }

		private static double GetCostMultiplier(GemsHudOfferTier tier, GemsHudOfferData data)
		{
            // Native ELF0x14c96dc.
            return tier == GemsHudOfferTier.Large ? data.costMultLarge : tier == GemsHudOfferTier.Medium ? data.costMultMedium : data.costMultSmall;
        }

		private static double CalculateStatValue(int cost, double rpm, GemsHudOfferData data)
		{
            // Native ELF0x14c9724.
            double gemValue = data.baseGemValue * (double)cost;
            if (!((double)cost <= data.highCostThreshold)) gemValue *= data.highCostLogBase * Math.Log((double)cost / data.highCostThreshold, 2.0) + 1.0;
            float minimum = data.minExtraValuePercent; float random = UnityEngine.Random.value;
            float extra = minimum + random * (data.maxExtraValuePercent - data.minExtraValuePercent);
            return Math.Round(gemValue * rpm * ((double)extra + 1.0));
        }
        // ARM FCVTZS plus explicit original +Infinity guard; normal inputs are original Math.Round outputs.
        private static int NativeInt(double value) => double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;

	}
}
