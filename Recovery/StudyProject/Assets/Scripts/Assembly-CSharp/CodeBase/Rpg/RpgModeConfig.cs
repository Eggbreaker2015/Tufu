using System.Collections.Generic;
using Codebase.Core;
using Codebase.Pet;
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
			new HeroDefinition { Key = "protagonist", IsProtagonist = true, DisplayName = "Pudge" },
			new HeroDefinition { Key = "golem", PetItemKey = "golem", AttackItemKey = "golem", HealthRatio = 1.6f, AttackRatio = 0.6f, DefenceRatio = 1.5f, DisplayName = "Golem" },
			new HeroDefinition { Key = "cultist", PetItemKey = "cultist", AttackItemKey = "cultist", HealthRatio = 0.8f, AttackRatio = 0.7f, DefenceRatio = 0.8f, DisplayName = "Cultist" }
		};
		public List<string> StartingHeroKeys = new List<string> { "protagonist", "golem", "cultist" };
		public float SwitchCooldown = 0.3f;
		public float SwitchTeleportGrace = 1f;
		public float AiAcceleration = 20f;
		public float AiAngularSpeed = 720f;
		public int LevelCap = 60;
		public double XpBase = 20;
		public double XpGrowth = 1.1;
		public double XpNeedBase = 15;
		public double XpNeedPerLevel = 3;
		public List<float> XpDifferenceFactors = new List<float> { 1f, 0.9f, 0.75f, 0.55f, 0.35f, 0.15f, 0f };
		public Sprite GoldIcon;
		public double GoldBase = 5;
		public double GoldGrowth = 1.1;
		public float HeroLevelGrowth = 1.08f;
		public float EnemyLevelGrowth = 1.08f;
		public float EnemyHealthScale = 1f;
		public float EnemyDamageScale = 1f;
		public float BaseHealthBonus = 20f;
		public float BaseDefenceBonus = 2f;
		public float NormalCooldown = 45f, EliteCooldown = 180f, BossCooldown = 600f, StaticCooldown = 120f;
		public float NormalXpMultiplier = 1f, EliteXpMultiplier = 5f, BossXpMultiplier = 20f, StaticXpMultiplier = 0f;
		public float NormalGoldMultiplier = 1f, EliteGoldMultiplier = 5f, BossGoldMultiplier = 20f, StaticGoldMultiplier = 0.5f;
		public float QuestStepXpMultiplier = 5f, QuestChainXpMultiplier = 20f;
		public List<int> IslandStarts = new List<int> { 1, 5, 10, 13, 17, 20, 23, 27, 30, 33, 37, 40, 43, 47, 50, 53, 57 };
		public List<int> IslandEnds = new List<int> { 5, 10, 13, 17, 20, 23, 27, 30, 33, 37, 40, 43, 47, 50, 53, 57, 60 };

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
