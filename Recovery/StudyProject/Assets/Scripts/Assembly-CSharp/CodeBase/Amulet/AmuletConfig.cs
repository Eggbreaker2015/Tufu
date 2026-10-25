using System.Collections.Generic;
using Codebase.Amulet.Upgrades;
using Codebase.Simulation;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Amulet
{
	public class AmuletConfig : ScriptableObject
	{
		private static readonly HashSet<StatType> AutoBalanceStatTypes;

		public int UnlockLocationNumber;

		public int MaxLevel;

		public int FeatureUnlockDay;

		public List<int> LevelTargetDays;

		public List<double> RequiredExperiencePerLevel;

		public AmuletUpgradeCostMultipliers UpgradeCostMultipliers;

		public AmuletUpgradeStatMultipliers StatModifierMultipliers;

		public AmuletUpgradeConfig UpgradeConfig;

		public double AutoBalanceCostMultiplier;

		public List<double> AutoBalanceExperienceMultipliers;

		public List<SoulDef> SoulDefs;

		public float PassDoubleSoulChance;

		public int PassDailyBossTickets;

		public int PassDailyLegendarySouls;

		public int PassOneTimeDivineSouls;

		public int PassDurationDays;

		public Sprite PassSoulDropIcon;

		public double GetRequiredExperience(int level)
		{
// Native ELF VA 0x15b1318 uses level - 2 and returns zero outside [2,MaxLevel].
            return level > 1 && level <= MaxLevel ? RequiredExperiencePerLevel[level - 2] : 0.0;
		}

		public void BakeAmuletAutoBalance(List<AmuletLevelBalanceData> levelBalanceData, List<SimulationSnapshot> timeChunkSnapshots)
		{
		}

		public SoulDef GetSoulDef(string soulKey)
		{
// Original ELF VA 0x15b2518: first matching definition, null if not found.
            foreach (SoulDef definition in SoulDefs) if (definition.Key == soulKey) return definition;
            return null;
		}

		public double GetBaseExperience(string soulKey)
		{
// Original ELF VA 0x15b26e8.
            return GetSoulDef(soulKey)?.BaseExperience ?? 1.0;
		}

		private void SaveAsset()
		{
		}
	}
}
