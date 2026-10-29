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
			new HeroDefinition { Key = "golem", PetItemKey = "golem", AttackItemKey = "golem", HealthRatio = 1.6f, AttackRatio = 0.7f, DefenceFlat = 2f, DisplayName = "Golem" },
			new HeroDefinition { Key = "cultist", PetItemKey = "cultist", AttackItemKey = "cultist", HealthRatio = 0.8f, AttackRatio = 0.9f, DisplayName = "Cultist" }
		};
		public List<string> StartingHeroKeys = new List<string> { "protagonist", "golem", "cultist" };
		public float SwitchCooldown = 0.3f;
		public float SwitchTeleportGrace = 1f;
		public float AiAcceleration = 20f;
		public float AiAngularSpeed = 720f;

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
