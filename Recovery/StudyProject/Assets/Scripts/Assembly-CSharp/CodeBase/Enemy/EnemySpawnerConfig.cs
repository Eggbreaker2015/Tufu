using System;
using System.Collections.Generic;
using Codebase.Amulet;
using Codebase.Economy;
using Codebase.Gates;
using Codebase.Player.Inventory;
using Codebase.Unit;
using Codebase.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Codebase.Enemy
{
	[Serializable]
	public class EnemySpawnerConfig
	{
		public string Key;

		public EnemyUnit Prefab;

		public Vector2 SpawnArea;

		public float SpawnAreaRotation;

		public int Count;

		public List<Stat> Stats;

		public WeaponType WeaponType;

		public ArmorConfig ArmorConfig;

		public float Cooldown;

		public float BigCooldown;

		public int ChargesBeforeBigRespawn;

		public bool SyncBigRespawnToUtc;

		public bool ShowCooldown;

		public float AggroRange;

		public float AggroDuration;

		public bool IsStatic;

		public bool DisableAfterFirstClear;

		public int LocationNumber;

		public List<StatModifier> StatRewards;

		public ItemConfig ItemReward;

		[KeysDropdown(typeof(ItemSlotKeys))]
		public string SlotReward;

		public int PortalGemReward;

		[KeysDropdown(typeof(SoulKeys))]
		public string SoulReward;

		public bool SoulAlwaysAvailable;

		[KeysDropdown(typeof(CurrencyKeys))]
		public string CurrencyReward;

		public double CurrencyRewardAmount = 1.0;

		public List<Gate> Gates;

		public int RewardedPoolIndex;

		public bool IsDays;

		public float OptimumTime;

		public float RelativeOptimumTime;

		public float AverageFightTime;

		public float PriorityMultiplier = 1f;

		public bool HandBalance;

		public float Difficulty = 1f;

		public bool IsEliteBoss;

		public bool IsVip;

		public int RequiredVipLevel;

		public bool IsEvent;

		public EventSpawnerDef EventDef;

		public bool NonSelectable;

		public bool IsSpecial;

		public bool IsFlying;

		public bool IsDummy;

		public float WanderRadius;

		public float WanderDwellTime;

		[FormerlySerializedAs("IsEnemyBoss")]
		[SerializeField]
		public bool IsBossFightPart;
	}
}
