using System.Collections.Generic;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Inventory;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Codebase.Rpg
{
	public class RpgModeConfig : ScriptableObject
	{
		private const string ResourcePath = "Rpg Mode Config";

		private static RpgModeConfig instance;
		private static bool loaded;

		[SerializeField]
		private bool enabled = true;

		public List<HeroDefinition> Heroes = new List<HeroDefinition>
		{
			new HeroDefinition { Key = "protagonist", IsProtagonist = true, Role = HeroRole.Protagonist, DisplayName = "Pudge" },
			new HeroDefinition { Key = "golem", Role = HeroRole.Tank, PetItemKey = "golem", AttackItemKey = "golem", HealthRatio = 1.6f, AttackRatio = 0.6f, DefenceRatio = 1.5f, DisplayName = "Golem" },
			new HeroDefinition { Key = "cultist", Role = HeroRole.MeleeDps, PetItemKey = "cultist", AttackItemKey = "cultist", HealthRatio = 0.8f, AttackRatio = 0.7f, DefenceRatio = 0.8f, DisplayName = "Cultist" },
			new HeroDefinition { Key = "pick", PetItemKey = "pick", AttackItemKey = "pick", Role = HeroRole.Tank, HealthRatio = 1.6f, AttackRatio = 0.6f, DefenceRatio = 1.5f, DisplayName = "重甲矛兵" },
			new HeroDefinition { Key = "mailbox", PetItemKey = "mailbox", AttackItemKey = "mailbox", Role = HeroRole.Bruiser, HealthRatio = 1.3f, AttackRatio = 0.85f, DefenceRatio = 1.2f, DisplayName = "棍棒兵" },
			new HeroDefinition { Key = "fork", PetItemKey = "fork", AttackItemKey = "fork", Role = HeroRole.Guard, HealthRatio = 1.2f, AttackRatio = 0.8f, DefenceRatio = 1.2f, DisplayName = "矛卫" },
			new HeroDefinition { Key = "knife", PetItemKey = "knife", AttackItemKey = "knife", Role = HeroRole.MeleeDps, HealthRatio = 1f, AttackRatio = 1.1f, DefenceRatio = 0.9f, DisplayName = "斧头兵" },
			new HeroDefinition { Key = "minisaw", PetItemKey = "minisaw", AttackItemKey = "minisaw", Role = HeroRole.CritDps, HealthRatio = 0.9f, AttackRatio = 1.25f, DefenceRatio = 0.8f, DisplayName = "剑士" },
			new HeroDefinition { Key = "pike", PetItemKey = "pike", AttackItemKey = "pike", Role = HeroRole.ReachDps, HealthRatio = 1f, AttackRatio = 1.15f, DefenceRatio = 0.9f, DisplayName = "长矛兵" }
		};
		public int NormalShards = 1, EliteShards = 3, BossShards = 10;
		public List<int> StarCopyThresholds = new List<int> { 10, 30, 70, 150 };
		public float StarStatBonus = 0.12f;
		public float TargetLockExtraRange = 4f;
		public float RetaliateWindow = 3f;
		public float TauntEliteDurationScale = 0.5f;
		public float SwitchCooldown = 0.3f;
		public float SwitchTeleportGrace = 1f;
		public float AiAcceleration = 20f;
		public float AiAngularSpeed = 720f;
		public int LevelCap = 60;
		public double XpBase = 20;
		public double XpGrowth = 1.1;
		public double XpNeedBase = 1.5;
		public double XpNeedPerLevel = 0.3;
		public List<float> XpDifferenceFactors = new List<float> { 1f };
		public Sprite GoldIcon;
		public double GoldBase = 5;
		public double GoldGrowth = 1.1;
		public float HeroLevelGrowth = 1.08f;
		public float EnemyLevelGrowth = 1.08f;
		public float EnemyHealthScale = 1f;
		public float EnemyDamageScale = 1f;
		public float BaseHealthBonus = 20f;
		public float BaseDefenceBonus = 2f;
		// Pudge's model is about twice as wide as his NavMeshAgent body, which the level-up effect is sized from.
		public float ProtagonistLevelUpVfxScale = 2f;
		public float NormalCooldown = 45f, EliteCooldown = 180f, BossCooldown = 600f, StaticCooldown = 120f;
		public float NormalXpMultiplier = 1f, EliteXpMultiplier = 5f, BossXpMultiplier = 20f, StaticXpMultiplier = 0.5f;
		public float NormalGoldMultiplier = 1f, EliteGoldMultiplier = 5f, BossGoldMultiplier = 20f, StaticGoldMultiplier = 0.5f;
		public float QuestStepXpMultiplier = 5f, QuestChainXpMultiplier = 20f;
		public List<int> IslandStarts = new List<int> { 1, 5, 10, 13, 17, 20, 23, 27, 30, 33, 37, 40, 43, 47, 50, 53, 57 };
		public List<int> IslandEnds = new List<int> { 5, 10, 13, 17, 20, 23, 27, 30, 33, 37, 40, 43, 47, 50, 53, 57, 60 };
		public float DungeonPortalRadius = 2f;
		public List<Dungeon.RpgDungeonDef> Dungeons = new List<Dungeon.RpgDungeonDef>
		{
			new Dungeon.RpgDungeonDef
			{
				Key = "dungeon.island1", DisplayName = "幽暗地穴", Island = 1, PortalPosition = new Vector3(5f, 0.05f, -62f), ArenaKey = "atlantic",
				Packs = new List<Dungeon.RpgDungeonPack>
				{
					new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_1 (2)_b2760d4c1e30b1b478fc09098d1ce40b", Along = 16f, Count = 3, Level = 5 },
					new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_common_cut_1d67c9a6468702a45ba35061fec7f723", Along = 30f, Count = 2, Level = 5 },
					new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_1 (2)_b2760d4c1e30b1b478fc09098d1ce40b", Along = 44f, Count = 3, Level = 5 },
					new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_common_cut_1d67c9a6468702a45ba35061fec7f723", Along = 56f, Count = 3, Level = 5 },
					new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_1 (2)_b2760d4c1e30b1b478fc09098d1ce40b", Along = 66f, Side = -14f, Count = 3, Level = 5 },
					new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_common_cut_1d67c9a6468702a45ba35061fec7f723", Along = 66f, Side = 14f, Count = 2, Level = 5 }
				},
				Boss = new Dungeon.RpgDungeonPack { SpawnerKey = "Spawner_boss_pier_common_slot_3a6eab0d4328bc3408bb980099f1c5ee", Count = 1, Level = 6 }
			}
		};

		public static RpgModeConfig Instance
		{
			get
			{
				if (!loaded)
				{
					instance = Resources.Load<RpgModeConfig>(ResourcePath);
					loaded = true;
				}
				return instance;
			}
		}

		public HeroDefinition ResolveHero(ItemConfig item)
		{
			return Heroes.Find(hero => hero != null && hero.PetItemKey == item.Key) ?? new HeroDefinition { Key = item.Key, PetItemKey = item.Key, AttackItemKey = item.Key, Role = HeroRole.MeleeDps };
		}

		public int ShardsFor(RpgSpawnerKind kind)
		{
			switch (kind) { case RpgSpawnerKind.Elite: return EliteShards; case RpgSpawnerKind.Boss: return BossShards; default: return NormalShards; }
		}

		public static bool Enabled => Instance != null && Instance.enabled && PetConfig.Enabled;

		[StaticReset]
		private static void ResetStaticState()
		{
			instance = null;
			loaded = false;
		}

#if UNITY_EDITOR
		[MenuItem("Tools/RPG/Create Config")]
		private static void CreateConfig()
		{
			const string path = "Assets/Resources/Rpg Mode Config.asset";
			if (AssetDatabase.LoadAssetAtPath<RpgModeConfig>(path) != null)
				return;
			AssetDatabase.CreateAsset(CreateInstance<RpgModeConfig>(), path);
			AssetDatabase.SaveAssets();
		}
#endif
	}
}
