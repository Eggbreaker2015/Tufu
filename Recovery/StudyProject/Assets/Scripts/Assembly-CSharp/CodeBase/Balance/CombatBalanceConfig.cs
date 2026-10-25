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
		}

		public int GetEvolutionShardsRequirement(bool isArmor, Rarity rarity, int evolutionLevel)
		{
			var existingLevel = GetExistingEvolutionLevel(evolutionLevel);
			return (isArmor ? EvolutionArmorShardsRequirements : EvolutionDamageShardsRequirements)
				.First(value => value.Rarity == rarity && value.EvolutionLevel == existingLevel).Value;
		}

		public void ApplyEvolutionAutoBalance(bool isArmor, WeaponType damageType, Rarity rarity, int evolutionLevel, double heuristicValue, double currentStatValue, int timeChunkIndex)
		{
		}

		private void FillRemainingEvolutionValues(bool isArmor, WeaponType damageType, Rarity rarity)
		{
		}

		private void SetEvolutionValue(List<EvolutionCombatValue> list, WeaponType damageType, Rarity rarity, int evolutionLevel, float value, float copyValue, int referenceShardsRequirement, int referenceStatPercent, int referenceTimeChunkIndex, float referenceStatValue, float referenceHeuristicValue)
		{
		}

		private float[] GetEvolutionTargetTimings(Rarity rarity)
		{
			return null;
		}

		private float[] GetArmorEvolutionTargetTimings(Rarity rarity)
		{
			return null;
		}

		private void TryCaptureShardsRequirement(List<EvolutionShardsRequirement> list, Rarity rarity, int evolutionLevel, List<ItemModel> items, bool isArmor, float playtime, float targetTiming)
		{
		}

		private void SetShardsValue(List<EvolutionShardsRequirement> list, Rarity rarity, int evolutionLevel, int value)
		{
		}

		private float[] ConvertIslandRowToDayTimings(EvolutionIslandTimingRow row)
		{
			return null;
		}

		public float ConvertIslandTimingToDays(float islandTiming)
		{
			return 0f;
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
