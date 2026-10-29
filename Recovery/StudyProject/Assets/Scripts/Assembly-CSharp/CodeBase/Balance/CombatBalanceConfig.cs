using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using UnityEngine;
using UnityEngine.Serialization;

namespace Codebase.Balance
{
	[CreateAssetMenu(menuName = "Balance/Combat Balance Config")]
	public class CombatBalanceConfig : ScriptableObject
	{
		private readonly HashSet<string> RebuildCapturedShards = new HashSet<string>();

		private const int InitialShardsRequirement = 1000000000;

		private const int EvolutionTimingLevelsCount = 15;

		public double MinCopyValueGrowthMultiplier;

		public double CopyValueTargetShare;

		public double MinEvolutionValueFromShardsMultiplier;

		public double MinArmorEvolutionValueFromPreviousEvolutionMultiplier;

		public int MaxEvolutionLevel;

		private static readonly Rarity[] TimedRarities = new[] { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary };

		private static readonly WeaponType[] UsedDamageTypes = new[] { WeaponType.Stabbing, WeaponType.Cutting, WeaponType.Buckshot, WeaponType.Universal };

		[FormerlySerializedAs("IslandTargetTimings")]
		public float[] IslandTargetDays;

		public float[] CommonEvolutionTargetTimings;

		public float[] UncommonEvolutionTargetTimings;

		public float[] RareEvolutionTargetTimings;

		public float[] EpicEvolutionTargetTimings;

		public float[] LegendaryEvolutionTargetTimings;

		public float[] CommonArmorEvolutionTargetTimings;

		public float[] UncommonArmorEvolutionTargetTimings;

		public float[] RareArmorEvolutionTargetTimings;

		public float[] EpicArmorEvolutionTargetTimings;

		public float[] LegendaryArmorEvolutionTargetTimings;

		public float[] CutDamageBaseByRarity;

		public float[] PierceDamageBaseByRarity;

		public float[] SmashDamageBaseByRarity;

		public float[] CutDamageGrowthPerLevelByRarity;

		public float[] PierceDamageGrowthPerLevelByRarity;

		public float[] SmashDamageGrowthPerLevelByRarity;

		public float[] ArmorBaseByRarity;

		public float[] ArmorGrowthPerLevelByRarity;

		public List<EvolutionCombatValue> EvolutionDamageValues;

		public List<EvolutionCombatValue> EvolutionArmorValues;

		public List<EvolutionShardsRequirement> EvolutionDamageShardsRequirements;

		public List<EvolutionShardsRequirement> EvolutionArmorShardsRequirements;

		public List<MythicalEvolutionValue> MythicalEvolutionValues;

		public bool SyncShieldValuesByLeader;

		public BalanceInterpolationsConfig Interpolations;

		[HideInInspector]
		public bool ShowIslandEvolutionTimingTable;

		public List<EvolutionIslandTimingRow> EvolutionIslandTimingRows;

		public List<EvolutionIslandFillRuleRow> EvolutionIslandFillRuleRows;

		public void ToggleIslandEvolutionTimingTable()
		{
		}

		public void FillIslandTimingsByStartStep()
		{
		}

		public void ApplyIslandTimingsToDayTimings()
		{
		}

		public void FillIslandTargetDaysByGateSpawners()
		{
		}

		public void ResetItemCostAndValues()
		{
		}

		public void FillMythicalEvolutionValues()
		{
		}

		public void FillRemainingEvolutionsByLastValue()
		{
		}

		public void PrintCurrentEvolutionBalanceByItems()
		{
		}

		public void UpdateEvolutionShardsRequirements(float playtime, List<ItemModel> items)
		{
            foreach (Rarity rarity in TimedRarities)
                for (int level = 1; level <= MaxEvolutionLevel; level++)
                {
                    TryCaptureShardsRequirement(EvolutionDamageShardsRequirements, rarity, level, items, false, playtime, GetEvolutionTargetTimings(rarity)[level - 1]);
                    TryCaptureShardsRequirement(EvolutionArmorShardsRequirements, rarity, level, items, true, playtime, GetArmorEvolutionTargetTimings(rarity)[level - 1]);
                }
		}

		public void PrintInterpolationResult(float timing)
		{
		}

		public void ApplyInterpolationIslandsToTimings()
		{
		}

		public float GetInterpolationCoefficient(float timing, BalanceInterpolationType type)
		{
// Original ELF VA 0x157dd80.
            return Interpolations.GetTotalCoefficient(timing, type, Rarity.Rare);
		}

		public float GetInterpolationCoefficient(float timing, BalanceInterpolationType type, Rarity rarity)
		{
// Original ELF VA 0x157dd98.
            return Interpolations.GetTotalCoefficient(timing, type, rarity);
		}

		private int GetExistingEvolutionLevel(int evolutionLevel)
		{
			return evolutionLevel < MaxEvolutionLevel ? evolutionLevel : MaxEvolutionLevel;
		}

		public void FillEvolutionTargetTimingsByFormula()
		{
		}

		public float GetDamageBase(WeaponType damageType, Rarity rarity)
		{
			return (damageType == WeaponType.Buckshot ? SmashDamageBaseByRarity :
				damageType == WeaponType.Cutting ? CutDamageBaseByRarity : PierceDamageBaseByRarity)[(int)rarity];
		}

		public float GetDamageGrowthPerLevel(WeaponType damageType, Rarity rarity)
		{
			return (damageType == WeaponType.Buckshot ? SmashDamageGrowthPerLevelByRarity :
				damageType == WeaponType.Cutting ? CutDamageGrowthPerLevelByRarity : PierceDamageGrowthPerLevelByRarity)[(int)rarity];
		}

		public float GetEvolutionDamage(WeaponType damageType, Rarity rarity, int evolutionLevel)
		{
			var existingLevel = GetExistingEvolutionLevel(evolutionLevel);
			return EvolutionDamageValues.First(value => value.DamageType == damageType && value.Rarity == rarity && value.EvolutionLevel == existingLevel).Value;
		}

		public float GetEvolutionDamageCopyValue(WeaponType damageType, Rarity rarity, int evolutionLevel)
		{
			var existingLevel = GetExistingEvolutionLevel(evolutionLevel);
			return EvolutionDamageValues.First(value => value.DamageType == damageType && value.Rarity == rarity && value.EvolutionLevel == existingLevel).CopyValue;
		}

		public float GetArmorBase(WeaponType damageType, Rarity rarity)
		{
			return ArmorBaseByRarity[(int)rarity];
		}

		public void TrySetArmorBaseValue(Rarity rarity, float value)
		{
            if (ArmorBaseByRarity[(int)rarity] == 0f) ArmorBaseByRarity[(int)rarity] = value;
		}

		public float GetArmorGrowthPerLevel(WeaponType damageType, Rarity rarity)
		{
			return ArmorGrowthPerLevelByRarity[(int)rarity];
		}

		public float GetEvolutionArmor(WeaponType damageType, Rarity rarity, int evolutionLevel)
		{
			var existingLevel = GetExistingEvolutionLevel(evolutionLevel);
			return EvolutionArmorValues.First(value => value.DamageType == damageType && value.Rarity == rarity && value.EvolutionLevel == existingLevel).Value;
		}

		public float GetEvolutionArmorCopyValue(WeaponType damageType, Rarity rarity, int evolutionLevel)
		{
			var existingLevel = GetExistingEvolutionLevel(evolutionLevel);
			return EvolutionArmorValues.First(value => value.DamageType == damageType && value.Rarity == rarity && value.EvolutionLevel == existingLevel).CopyValue;
		}

		public void SetEvolutionValue(bool isArmor, WeaponType damageType, Rarity rarity, int evolutionLevel, float value, float copyValue, int referenceShardsRequirement, int referenceStatPercent)
		{
            SetEvolutionValue(isArmor ? EvolutionArmorValues : EvolutionDamageValues, damageType, rarity, evolutionLevel,
                value, copyValue, referenceShardsRequirement, referenceStatPercent, 0, 0f, 0f);
		}

		public int GetEvolutionShardsRequirement(bool isArmor, Rarity rarity, int evolutionLevel)
		{
			var existingLevel = GetExistingEvolutionLevel(evolutionLevel);
			return (isArmor ? EvolutionArmorShardsRequirements : EvolutionDamageShardsRequirements)
				.First(value => value.Rarity == rarity && value.EvolutionLevel == existingLevel).Value;
		}

		public void ApplyEvolutionAutoBalance(bool isArmor, WeaponType damageType, Rarity rarity, int evolutionLevel, double heuristicValue, double currentStatValue, int timeChunkIndex)
		{
            int shards = GetEvolutionShardsRequirement(isArmor, rarity, evolutionLevel);
            if (shards <= 0) throw new ArgumentOutOfRangeException(nameof(shards));
            double previous = evolutionLevel == 1 ? (isArmor ? GetArmorBase(damageType, rarity) : GetDamageBase(damageType, rarity))
                : isArmor ? GetEvolutionArmor(damageType, rarity, evolutionLevel - 1) : GetEvolutionDamage(damageType, rarity, evolutionLevel - 1);
            double growth = evolutionLevel == 1 ? (isArmor ? GetArmorGrowthPerLevel(damageType, rarity) : GetDamageGrowthPerLevel(damageType, rarity))
                : isArmor ? GetEvolutionArmorCopyValue(damageType, rarity, evolutionLevel - 1) : GetEvolutionDamageCopyValue(damageType, rarity, evolutionLevel - 1);
            double target = Math.Max(heuristicValue, previous + MinEvolutionValueFromShardsMultiplier * growth * shards);
            if (isArmor) target = Math.Max(target, MinArmorEvolutionValueFromPreviousEvolutionMultiplier * previous);
            float value = (float)RebuildNice(target);
            double share = CopyValueTargetShare * (heuristicValue - previous) / shards;
            float copy = (float)Math.Max(MinCopyValueGrowthMultiplier * growth, share > 0 ? RebuildNice(share) : 0.0);
            SetEvolutionValue(isArmor ? EvolutionArmorValues : EvolutionDamageValues, damageType, rarity, evolutionLevel, value, copy,
                shards, currentStatValue == 0 ? 0 : (int)Math.Round(100.0 * value / currentStatValue), timeChunkIndex, (float)currentStatValue, (float)heuristicValue);
		}

		private void FillRemainingEvolutionValues(bool isArmor, WeaponType damageType, Rarity rarity)
		{
		}

        private static double RebuildNice(double value)
        {
            if (!(value > 0)) return 0;
            double power = Math.Pow(10, Math.Floor(Math.Log10(value)));
            double mantissa = value / power;
            double step = mantissa < 4 ? power / 10 : mantissa < 8 ? power / 2 : power;
            return Math.Round(value / step) * step;
        }

        private void SetEvolutionValue(List<EvolutionCombatValue> list, WeaponType damageType, Rarity rarity, int evolutionLevel, float value, float copyValue, int referenceShardsRequirement, int referenceStatPercent, int referenceTimeChunkIndex, float referenceStatValue, float referenceHeuristicValue)
        {
            var row = new EvolutionCombatValue { DamageType = damageType, Rarity = rarity, EvolutionLevel = evolutionLevel,
                Value = value, CopyValue = copyValue, ReferenceShardsRequirement = referenceShardsRequirement, ReferenceStatPercent = referenceStatPercent,
                ReferenceTimeChunkIndex = referenceTimeChunkIndex, ReferenceStatValue = referenceStatValue, ReferenceHeuristicValue = referenceHeuristicValue };
            int index = list.FindIndex(x => x.DamageType == damageType && x.Rarity == rarity && x.EvolutionLevel == evolutionLevel);
            if (index < 0) list.Add(row); else list[index] = row;
        }

		private float[] GetEvolutionTargetTimings(Rarity rarity)
		{
            return new[] { CommonEvolutionTargetTimings, UncommonEvolutionTargetTimings, RareEvolutionTargetTimings, EpicEvolutionTargetTimings, LegendaryEvolutionTargetTimings }[(int)rarity];
		}

		private float[] GetArmorEvolutionTargetTimings(Rarity rarity)
		{
            return new[] { CommonArmorEvolutionTargetTimings, UncommonArmorEvolutionTargetTimings, RareArmorEvolutionTargetTimings, EpicArmorEvolutionTargetTimings, LegendaryArmorEvolutionTargetTimings }[(int)rarity];
		}

		private void TryCaptureShardsRequirement(List<EvolutionShardsRequirement> list, Rarity rarity, int evolutionLevel, List<ItemModel> items, bool isArmor, float playtime, float targetTiming)
		{
            string key = isArmor + ":" + (int)rarity + ":" + evolutionLevel;
            if (playtime < targetTiming || RebuildCapturedShards.Contains(key)) return;
            var eligible = items.Where(x => x.IsArmor == isArmor && x.Config.Rarity == rarity && (x.Evolved ? x.Level : 0) == evolutionLevel - 1).ToList();
            if (eligible.Count == 0) return;
            SetShardsValue(list, rarity, evolutionLevel, Math.Max(1, (int)RebuildNice(eligible.Max(x => x.Copies))));
            RebuildCapturedShards.Add(key);
		}

		private void SetShardsValue(List<EvolutionShardsRequirement> list, Rarity rarity, int evolutionLevel, int value)
		{
            int index = list.FindIndex(x => x.Rarity == rarity && x.EvolutionLevel == evolutionLevel);
            var row = new EvolutionShardsRequirement { Rarity = rarity, EvolutionLevel = evolutionLevel, Value = value };
            if (index < 0) list.Add(row); else list[index] = row;
		}

		private float[] ConvertIslandRowToDayTimings(EvolutionIslandTimingRow row)
		{
			return null;
		}

		public float ConvertIslandTimingToDays(float islandTiming)
		{
            if (islandTiming < 1f || islandTiming > IslandTargetDays.Length) throw new ArgumentOutOfRangeException(nameof(islandTiming));
            int i = (int)Math.Floor(islandTiming) - 1;
            if (i == IslandTargetDays.Length - 1) return IslandTargetDays[i];
            return IslandTargetDays[i] + (IslandTargetDays[i + 1] - IslandTargetDays[i]) * (islandTiming - i - 1f);
		}

		private static List<EvolutionIslandTimingRow> CreateEvolutionIslandTimingRows()
		{
			return null;
		}

		private static List<MythicalEvolutionValue> CreateMythicalEvolutionValues()
		{
			return null;
		}

		private void ResetArmorBaseValues()
		{
		}

		private MythicalEvolutionValue GetMythicalEvolutionValue(int evolutionLevel)
		{
			return default;
		}

		internal static Color GetRarityRowColor(Rarity rarity)
		{
			return default;
		}
	}
}
