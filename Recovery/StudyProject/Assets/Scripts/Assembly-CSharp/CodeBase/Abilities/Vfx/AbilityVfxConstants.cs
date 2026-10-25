namespace Codebase.Abilities.Vfx
{
	public static class AbilityVfxConstants
	{
		public static readonly AbilityVfxData HealSmall;

		public static readonly AbilityVfxData HealBig;

		public static readonly AbilityVfxData ApplyMark;

		public static readonly AbilityVfxData ApplyShock;

		public static readonly AbilityVfxData ShockLightning;

		public static readonly AbilityVfxData MegaLightning;

		public static readonly AbilityVfxData ConsumeShock;

		public static readonly AbilityVfxData Stunned;

		public static readonly AbilityVfxData DeathEvasion;

		public static readonly AbilityVfxData ReflectionStrike;

		public static readonly AbilityVfxData SwordHit;

		public static readonly AbilityVfxData SwordCrit;

		public static readonly AbilityVfxData SwordSuperCrit;

		public static readonly AbilityVfxData[] RageLevelUps;

        static AbilityVfxConstants()
        {
            // Native ELF VA 0x15e3c50. Keys resolved from ELF RELA; float payloads read directly.
            const string prefix = "Assets/Prefabs/Vfx/Abilities/";
            HealSmall = new AbilityVfxData(prefix + "HealSmall.prefab", 1f);
            HealBig = new AbilityVfxData(prefix + "HealBig.prefab", 1.5f);
            ApplyMark = new AbilityVfxData(prefix + "ApplyMark.prefab", 1f, 4f, 30f, -54f, -35f);
            ApplyShock = new AbilityVfxData(prefix + "ApplyShock.prefab", 1f, 1.5f);
            ShockLightning = new AbilityVfxData(prefix + "ShockLightning.prefab", 1f);
            MegaLightning = new AbilityVfxData(prefix + "MegaLightning.prefab", 1f);
            ConsumeShock = new AbilityVfxData(prefix + "ConsumeShock.prefab", 1f);
            Stunned = new AbilityVfxData(prefix + "Stunned.prefab", 1f);
            DeathEvasion = new AbilityVfxData(prefix + "DeathEvasion.prefab", 1.5f, 0f, 0f);
            ReflectionStrike = new AbilityVfxData(prefix + "ReflectionStrike.prefab", 1f, 1.5f, 0f);
            SwordHit = new AbilityVfxData(prefix + "SwordHit.prefab", 1f);
            SwordCrit = new AbilityVfxData(prefix + "SwordCrit.prefab", 1f);
            SwordSuperCrit = new AbilityVfxData(prefix + "SuperCrit_Blue.prefab", 1f);
            RageLevelUps = new[] {
                new AbilityVfxData(prefix + "RageLevelUp1.prefab", 1.5f, 5f),
                new AbilityVfxData(prefix + "RageLevelUp2.prefab", 1.5f, 5f),
                new AbilityVfxData(prefix + "RageLevelUp3.prefab", 1.5f, 5f)
            };
        }
	}
}
