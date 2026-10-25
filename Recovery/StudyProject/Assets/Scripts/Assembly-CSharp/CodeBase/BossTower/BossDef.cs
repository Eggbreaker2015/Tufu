using System;
using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Meta.Upgrades;
using Codebase.Rewards;
using Codebase.Unit;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class BossDef
	{
		[KeysDropdown(typeof(BossTowerBossKeys))]
		public string Key;

		public string ArenaKey;

		public string DisplayName;

		public Sprite FullPortrait;

		public float PortraitScale;

		public Vector3 PortraitItemOffset;

		public EnemyUnit Prefab;

		public List<Stat> Stats;

		public WeaponType WeaponType;

		public ArmorConfig ArmorConfig;

		public float AggroRange;

		public float AggroDuration;

		public bool IsFlying;

		[SerializeReference]
		public List<RewardBase> Rewards;

		[SerializeReference]
		public List<BossBehaviorConfig> Behaviors;

		public bool isUnlockedByDefault;

		[KeysDropdown(typeof(UpgradeKeys))]
		public string UnlockUpgradeKey;

		public float AverageFightTime;

		public float Difficulty;

		public float EliteAverageFightTime;

		public float EliteDifficulty;

		[KeysDropdown(typeof(UpgradeKeys))]
		public List<string> BossUpgradeKeys;
	}
}
