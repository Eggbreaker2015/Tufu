using System;
using System.Collections.Generic;
using Codebase.Enemy;

namespace Codebase.Simulation
{
	[Serializable]
	public class SpawnerBakeReportData
	{
		public SimulationSnapshot Snapshot;

		public string Key;

		public string Name;

		public int LocationNumber;

		public int TimeChunkIndex;

		public float Playtime;

		public float OptimumTime;

		public float ReCalcOptimumTime;

		public double PrevHealth;

		public double PrevDamage;

		public int Count;

		public float AverageFightTime;

		public WeaponType ArmorType;

		public float ArmorResistRaw;

		public float ArmorResistUsed;

		public WeaponType PreferredWeaponType;

		public double CritChanceRaw;

		public double CritDamageRaw;

		public double CritChance;

		public double CritDamage;

		public double DpsByArmorType;

		public double PlayerDpsBeforeCrit;

		public double CritDps;

		public double PlayerDps;

		public double DefenseFromSnapshot;

		public double Defense;

		public double HealthRegenFromStats;

		public double FlatRegenFromStats;

		public double BattleRegen;

		public double PlayerEvasion;

		public double PlayerBlockChanceRaw;

		public double PlayerBlockAmountRaw;

		public double PlayerBlockChance;

		public double PlayerBlockAmount;

		public double BlockDamageMultiplier;

		public float EnemiesCount;

		public float DifficultyBeforeValidation;

		public float DifficultyUsed;

		public double NeedTotalHpBaseHealth;

		public double NeedTotalHp;

		public double NeedDps;

		public double NeedDpsFromOneUnit;

		public double AttackCooldownBase;

		public double HitFrequency;

		public double NeedDamageFromOneUnit;

		public double IncomingDamagePercentMultiplier;

		public double PlayerAbilityDpsMultiplier;

		public double PlayerAblityEhpMultiplier;

		public double NewDamageRaw;

		public double NewDamage;

		public double NewHealthRaw;

		public double NewHealth;

		public List<SpawnerBakeArmorCandidateData> ArmorCandidateRows;
	}
}
