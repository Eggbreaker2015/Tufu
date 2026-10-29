using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Abilities.Effects;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Abilities
{
	// Declaration order is the priority: when an ability has several effects, the earliest category wins,
	// so a distinctive effect (lightning, stun, heal) beats a generic stat bonus on the same ability.
	public enum AbilityIconCategory
	{
		Lightning,
		Stun,
		Splash,
		Mark,
		Rage,
		Loot,
		Thorns,
		Lifesteal,
		Heal,
		Crit,
		AttackSpeed,
		Speed,
		Defense,
		Health,
		Damage,
		Power
	}

	// One icon per effect category, loaded from Resources/AbilityIcons/<key>.png.
	public static class AbilityIcons
	{
		private const string ResourceFolder = "AbilityIcons/";

		private static readonly Dictionary<AbilityDef, AbilityIconCategory> categoryCache = new Dictionary<AbilityDef, AbilityIconCategory>();

		private static readonly Dictionary<AbilityIconCategory, Sprite> spriteCache = new Dictionary<AbilityIconCategory, Sprite>();

		public static Sprite Get(AbilityDef def)
		{
			return def == null ? null : Get(GetCategory(def));
		}

		public static Sprite Get(AbilityIconCategory category)
		{
			if (spriteCache.TryGetValue(category, out Sprite sprite))
				return sprite;
			sprite = Resources.Load<Sprite>(ResourceFolder + GetKey(category));
			spriteCache[category] = sprite;
			return sprite;
		}

		public static AbilityIconCategory GetCategory(AbilityDef def)
		{
			if (categoryCache.TryGetValue(def, out AbilityIconCategory cached))
				return cached;
			AbilityIconCategory best = AbilityIconCategory.Power;
			if (def.Effects != null)
				foreach (IAbilityEffect effect in def.Effects)
					if (TryClassify(effect, out AbilityIconCategory category) && category < best)
						best = category;
			categoryCache[def] = best;
			return best;
		}

		public static string GetKey(AbilityIconCategory category)
		{
			switch (category)
			{
				case AbilityIconCategory.Lightning: return "lightning";
				case AbilityIconCategory.Stun: return "stun";
				case AbilityIconCategory.Splash: return "splash";
				case AbilityIconCategory.Mark: return "mark";
				case AbilityIconCategory.Rage: return "rage";
				case AbilityIconCategory.Loot: return "loot";
				case AbilityIconCategory.Thorns: return "thorns";
				case AbilityIconCategory.Lifesteal: return "lifesteal";
				case AbilityIconCategory.Heal: return "heal";
				case AbilityIconCategory.Crit: return "crit";
				case AbilityIconCategory.AttackSpeed: return "attack_speed";
				case AbilityIconCategory.Speed: return "speed";
				case AbilityIconCategory.Defense: return "defense";
				case AbilityIconCategory.Health: return "health";
				case AbilityIconCategory.Damage: return "damage";
				default: return "power";
			}
		}

		private static bool TryClassify(IAbilityEffect effect, out AbilityIconCategory category)
		{
			switch (effect)
			{
				case SpawnLightningEffect _:
				case LightningDamageBonusEffect _:
				case IncreaseLightningChanceEffect _:
				case ConsumeShockEffect _:
				case BonusDamagePerConsumedShockOnTargetEffect _:
					category = AbilityIconCategory.Lightning; return true;
				case StunEffect _:
				case BonusDamageToStunnedEffect _:
					category = AbilityIconCategory.Stun; return true;
				case PetSplashDamageEffect _:
					category = AbilityIconCategory.Splash; return true;
				case ApplyMarkEffect _:
				case IncreaseMarkChanceEffect _:
				case IncreaseMaxMarksEffect _:
					category = AbilityIconCategory.Mark; return true;
				case AddRageEffect _:
				case GrantRageAccessEffect _:
				case ResetRageOnLethalEffect _:
					category = AbilityIconCategory.Rage; return true;
				case DoubleLootEffect _:
				// "Static" targets are trees and other resources, not shocked enemies.
				case EmpowerHitAgainstStaticEffect _:
					category = AbilityIconCategory.Loot; return true;
				case ThornsEffect _:
				case BuffNextThornsDamageEffect _:
					category = AbilityIconCategory.Thorns; return true;
				case HealFromDamageEffect _:
				case TriggerLifestealEffect _:
				case BuffNextHitLifestealFlatEffect _:
					category = AbilityIconCategory.Lifesteal; return true;
				case HealPercentEffect _:
				case PetHealEffect _:
				case HealthRegenForMissingHealthEffect _:
					category = AbilityIconCategory.Heal; return true;
				case GuaranteedCritEffect _:
				case BuffNextHitSuperCritEffect _:
				case CritChancePerStackEffect _:
				case AddWeaponSuperCritChanceEffect _:
				case AddWeaponSuperCritDamageEffect _:
					category = AbilityIconCategory.Crit; return true;
				case AttackSpeedMultiplierEffect _:
				case AttackSpeedForEmptySlotsEffect _:
					category = AbilityIconCategory.AttackSpeed; return true;
				case LethalIgnorePassiveEffect _:
					category = AbilityIconCategory.Defense; return true;
				case EmpowerHitEffect _:
				case BuffNextHitDamageEffect _:
				case AllWeaponsDamageMultiplierEffect _:
				case DamageForEmptySlotsEffect _:
				case DamageForMissingHealthEffect _:
				case DamageForStatEffect _:
				case ScaledMultiplierEffect _:
				case TriggerHighestDamageWeaponEffect _:
					category = AbilityIconCategory.Damage; return true;
				case TemporaryWeaponBuffEffect buff:
					category = FromBuff(buff.BuffType); return true;
				case WeaponStatStackEffect stack:
					category = FromBuff(stack.BuffType); return true;
				case AddStatEffect stat:
					return TryFromStat(stat.StatType, out category);
				case AddStatPassiveEffect passive:
					return TryFromStat(passive.StatType, out category);
				case StatStackEffect stack:
					return TryFromStat(stack.StatType, out category);
				case GrantStatOnHitEffect onHit:
					return TryFromStat(onHit.StatType, out category);
				case BonusStatPerHitsTakenEffect hitsTaken:
					return TryFromStat(hitsTaken.StatType, out category);
				case GrantRandomStatPerDistanceEffect _:
					category = AbilityIconCategory.Speed; return true;
				default:
					// Ability boosts, cooldown resets and re-triggers fall back to the generic Power icon.
					category = AbilityIconCategory.Power; return false;
			}
		}

		private static AbilityIconCategory FromBuff(WeaponBuffType type)
		{
			switch (type)
			{
				case WeaponBuffType.AttackSpeed: return AbilityIconCategory.AttackSpeed;
				case WeaponBuffType.CritDamage:
				case WeaponBuffType.CritChance: return AbilityIconCategory.Crit;
				default: return AbilityIconCategory.Damage;
			}
		}

		private static bool TryFromStat(StatType stat, out AbilityIconCategory category)
		{
			switch (stat)
			{
				case StatType.MovementSpeed:
					category = AbilityIconCategory.Speed; return true;
				case StatType.Health:
					category = AbilityIconCategory.Health; return true;
				case StatType.HealthRegen:
					category = AbilityIconCategory.Heal; return true;
				case StatType.AttackCooldown:
					category = AbilityIconCategory.AttackSpeed; return true;
				case StatType.LifestealFlat:
				case StatType.LifestealPercent:
					category = AbilityIconCategory.Lifesteal; return true;
				case StatType.CritChance:
				case StatType.CritDamagePercent:
				case StatType.SuperCritChance:
				case StatType.SuperCritDamagePercent:
					category = AbilityIconCategory.Crit; return true;
				case StatType.StabDefenceFlat:
				case StatType.StabDefencePercent:
				case StatType.CutDefenceFlat:
				case StatType.CutDefencePercent:
				case StatType.MagicDefenceFlat:
				case StatType.MagicDefencePercent:
				case StatType.BuckshotDefenceFlat:
				case StatType.BuckshotDefencePercent:
				case StatType.UniversalDefenceFlat:
				case StatType.UniversalDefencePercent:
				case StatType.GlobalDefence:
				case StatType.Evasion:
				case StatType.BlockChance:
				case StatType.BlockAmount:
					category = AbilityIconCategory.Defense; return true;
				case StatType.VisualScaleMultiplier:
					category = AbilityIconCategory.Power; return false;
				default:
					category = AbilityIconCategory.Damage; return true;
			}
		}
	}
}
