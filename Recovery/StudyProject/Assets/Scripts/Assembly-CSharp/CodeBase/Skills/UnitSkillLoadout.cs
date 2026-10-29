using System;
using System.Collections.Generic;

namespace Codebase.Skills
{
	[Serializable]
	public class SkillSlotDef
	{
		public string SkillKey;

		public int Level = 1;
	}

	// Enemy: prefab name (pooled instance name without "(Clone)"); Boss: boss key (EnemySpawnerConfig.Key of boss fight units).
	[Serializable]
	public class UnitSkillLoadout
	{
		public SkillOwnerKind OwnerKind;

		public string Key;

		public List<SkillSlotDef> Skills = new List<SkillSlotDef>();

		// Enemies only: replaces the melee basic attack with a projectile. A loadout without this block stays melee.
		public RangedAttackDef RangedAttack = new RangedAttackDef();
	}

	// Ranged basic attack (EnemyAttackSystem + SkillService.LaunchRangedAttack). Installed per spawn by SkillService.InstallForEnemy.
	[Serializable]
	public class RangedAttackDef
	{
		public bool Enabled;

		// Distance at which the enemy stops and shoots; the effective range is max(AttackRange stat, this).
		public float AttackRange = 7.5f;

		// Effective aggro range = max(spawner AggroRange, this); keeps the enemy engaged at its shooting distance.
		public float AggroRange = 10f;

		// > 0: step back KiteDistance metres when the target comes closer than this (at most once per KiteCooldown). 0 = hold position.
		public float MinRange;

		public float KiteDistance = 3f;

		public float KiteCooldown = 4f;

		// SkillLibrary.Vfx key of the flying visual (a prefab, or a runtime projectile with ProjectileTexture). Empty = invisible, lands after the travel time.
		public string ProjectileVfxKey;

		public float ProjectileSpeed = 14f;

		// Optional SkillLibrary.Vfx key spawned on the target at impact.
		public string ImpactVfxKey;

		public float ImpactVfxScale = 1f;

		// 1 = exactly the melee hit (same WeaponHitEvent); otherwise WeaponHitEvent.SpecialMultiplier.
		public float DamageMultiplier = 1f;

		// Ground enemies only: a NavMesh.Raycast must reach the target, otherwise the enemy moves closer (ignored within 3 m).
		public bool RequireLineOfFire = true;
	}
}
