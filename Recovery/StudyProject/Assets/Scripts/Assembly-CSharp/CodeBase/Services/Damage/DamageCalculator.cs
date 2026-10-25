using System.Linq;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Abilities.Effects;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Rage;
using Codebase.Crystals.Objective;
using Codebase.Quests.Components;
using Codebase.Unit.Components;
using Codebase.Utilities;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Providers;
using System;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Unit;
using Codebase.Unit.Events;

namespace Codebase.Services.Damage
{
	public static class DamageCalculator
	{
		public static double ApplyDamageDefense(double attackDamage, double flatDefense)
		{
			if (!(attackDamage > 0.0)) return 0.0;
			var ratio = flatDefense / attackDamage;
			return Math.Exp(ratio * -0.75) * attackDamage * Math.Max(0.0, ratio * -0.25 + 1.0);
		}

		public static double SolveAttackDamage(double targetIncomingDamage, Func<double, double> forward)
		{
			double upper = 1.0;
			int remaining = int.MaxValue;
			do
			{
				if (targetIncomingDamage <= forward(upper)) break;
				upper += upper;
			} while (--remaining != 0);
			if (remaining == 0) UnityEngine.Debug.LogError(upper);
			double lower = 0.0;
			remaining = int.MaxValue;
			do
			{
				if (upper - lower <= 1.0) return upper;
				var middle = (upper + lower) * 0.5;
				if (targetIncomingDamage <= forward(middle)) upper = middle;
				else lower = middle;
			} while (--remaining != 0);
			UnityEngine.Debug.LogError(upper);
			return upper;
		}

		public static double SolveAttackDamageForIncomingDamage(double targetIncomingDamage, double flatDefense, double percentMultiplier)
		{
			return SolveAttackDamage(targetIncomingDamage, attack => ApplyDamageDefense(attack, flatDefense) * percentMultiplier);
		}

		public static double GetBattleRegen(double healthRegen, double lifestealFlat, double difficulty)
		{
			return (difficulty + 1.0) * healthRegen + (difficulty + difficulty + 3.0) * lifestealFlat;
		}

		public static DamageResult Calculate(WeaponHitEvent data, bool fromSimulation = false, double simulationFlatDefenceMultiplier = 1.0)
		{
			// Native ELF VA 0x140a4cc (bounded pcode: zero outside-body addresses).
			// The native branch is selected by Target's actual EnemyUnit type.
			var result = new DamageResult();
			if (!(data.Target is EnemyUnit enemy))
			{
			    StatType flatType = WeaponTypeToDefence(data.DamageType, false);
			    StatType percentType = WeaponTypeToDefence(data.DamageType, true);
			    double attack = data.Source.GetStatValue(StatType.Damage);
			    MetaStatsModel meta = GameContext.Get<MetaStatsModel>();
			    double universalFlat = data.Target.GetStatValue(StatType.UniversalDefenceFlat);
			    double universalPercent = data.Target.GetStatValue(StatType.UniversalDefencePercent);
			    var attacker = data.Source as EnemyUnit;
			    if (attacker != null && attacker.Config != null && attacker.Config.IsEvent)
			        attack *= (double)Math.Max(0f, attacker.EventDamageRampTimer - 5f) * 0.04 + 1.0;
			    if (attacker != null && attacker.Config != null && attacker.Config.IsBossFightPart)
			        attack /= meta.Get("tower_damage_reduction").Value + 1.0;
			    PlayerModel player = GameContext.Get<PlayerModel>();
			    double armor = player.EquippedArmors.Where(item => item.Config.DamageType == WeaponType.Universal || item.Config.DamageType == data.DamageType).Sum(item => item.Armor);
			    double typedFlat = data.Target.GetStatValue(flatType);
			    double typedPercent = data.Target.GetStatValue(percentType);
			    double flatScale = fromSimulation ? simulationFlatDefenceMultiplier : 1.0;
			    double incoming = ApplyDamageDefense(attack, flatScale * (universalFlat + armor + typedFlat) * typedPercent);
			    incoming = Math.Max((1.0 - universalPercent) * incoming, 0.0);
			    bool dodge = data.Target.GetStatSpecificValue(StatType.Evasion).Roll();
			    if (dodge && !data.Undodgeable)
			    {
			        incoming = 0.0;
			        result.HasDodged = true;
			    }
			    else
			    {
			        if (!data.Undodgeable && data.Target.GetStatSpecificValue(StatType.BlockChance).Roll())
			        {
			            result.HasBlocked = true;
			            result.BlockedDamageReduction = data.Target.GetStatSpecificValue(StatType.BlockAmount);
			            incoming *= 1.0 - result.BlockedDamageReduction;
			        }
			        if (incoming > 0.0)
			        {
			            int rage = GameContext.Get<RageModel>().ActiveRageLevel;
			            if (rage > 0)
			            {
			                double reduction = meta.Get("rage_damage_reduce").Value;
			                if (reduction > 0.0) incoming /= reduction * rage + 1.0;
			            }
			            incoming *= GetEnemyEventDamageMultiplier(data.Target, attacker);
			            if (attacker != null) incoming *= GetIslandMasteryIncomingMultiplier(attacker, meta);
			            incoming *= GetGlobalDefenceMultiplier(data.Target);
			        }
			    }
			    if (data.HasSpecialMultiplier) incoming *= data.SpecialMultiplier;
			    result.Damage = Math.Round(incoming);
			    // Preserve the original incoming branch's inverted resistance ratio.
			    result.Resist = result.Damage <= 0.0 ? 0.0 : 1.0 - attack / result.Damage;
			    return result;
			}

			PlayerModel owner = GameContext.Get<PlayerModel>();
			MetaStatsModel metaStats = GameContext.Get<MetaStatsModel>();
			Armor resistance = data.Armor ?? enemy.ArmorConfig.Armors.First(armor => armor.Type == data.DamageType);
			float resist = resistance.Resist;
			double baseAttack = data.Source.GetStatValue(StatType.Damage);
			double weaponDamage = data.Weapon.Damage;
			double flatDamage = data.Source.GetStatValue(data.Weapon.Config.DamageStatTypeFlat);
			double percentDamage = data.Source.GetStatValue(data.Weapon.Config.DamageStatTypePercent);
			double universalDefense = data.Target.GetStatValue(StatType.UniversalDefencePercent);
			double criticalDamage = data.Source.GetStatSpecificValue(StatType.CritDamagePercent);
			double criticalChance = enemy.SpecialAdditionalCritChancePercent + data.Source.GetStatSpecificValue(StatType.CritChance) + data.Weapon.AbilityCritChanceBonus;
			double superChance = data.Source.GetStatSpecificValue(StatType.SuperCritChance) + data.Weapon.AbilitySuperCritChanceBonus + owner.NextHitSuperCritChanceBonus + owner.BlockSuperCritChanceBonus;
			double superDamage = data.Source.GetStatSpecificValue(StatType.SuperCritDamagePercent) + data.Weapon.AbilitySuperCritDamageBonus + owner.NextHitSuperCritDamageBonus;
			result.IsCrit = data.Weapon.AbilityStates.Any(state => state.GuaranteedCrit) || criticalChance.Roll();
			result.IsSuperCrit = result.IsCrit && superChance.Roll();
			double eventMultiplier = GetPlayerEventDamageMultiplier(data.Source, enemy);
			double critMultiplier = fromSimulation ? (criticalDamage - 1.0) * criticalChance + 1.0 : criticalDamage;
			double damage = percentDamage * (baseAttack + weaponDamage + flatDamage + (double)(owner.AbilityDamageFlat + data.Weapon.AbilityDamageFlat)) * (double)(1f - resist);
			if (enemy.Config.IsBossFightPart) damage *= metaStats.Get("tower_damage").Value + 1.0;
			if (result.IsCrit || fromSimulation)
			{
			    float abilityCrit = 1f;
			    foreach (var state in data.Weapon.AbilityStates) abilityCrit *= state.GetWeaponStatMultiplier(WeaponBuffType.CritDamage);
			    damage = damage * critMultiplier * data.Weapon.AbilityCritMultiplier * abilityCrit;
			}
			damage *= result.IsSuperCrit ? superDamage : 1.0;
			foreach (var state in data.Weapon.AbilityStates)
			{
			    damage = damage * state.DamageMultiplier * state.GetWeaponStatMultiplier(WeaponBuffType.Damage);
			    if (state.EmpoweredHit > 0f)
			    {
			        float empower = state.EmpoweredHit;
			        state.EmpoweredHit = 0f;
			        damage *= (double)(empower + 1f);
			        result.IsEmpowered = true;
			    }
			}
			if (data.Weapon.NextHitEmpower > 0f)
			{
			    float empower = data.Weapon.NextHitEmpower;
			    data.Weapon.NextHitEmpower = 0f;
			    damage *= (double)(empower + 1f);
			    result.IsEmpowered = true;
			}
			foreach (var item in owner.EquippedItems)
			    foreach (var state in item.AbilityStates) state.HitsCounter++;
			damage = damage * data.Weapon.AbilityDamageMultiplier * owner.AbilityDamageMultiplier * Math.Max(0.0, 1.0 - universalDefense);
			if (owner.NextHitDamageBonus > 0f)
			{
			    damage *= (double)(owner.NextHitDamageBonus + 1f);
			    result.IsEmpowered = true;
			}
			damage = eventMultiplier * damage * GetGlobalAttackMultiplier(data.Source) * GetIslandMasteryOutgoingMultiplier(enemy, metaStats);
			if (owner.BonusDamageToStunned > 0f)
			{
			    EntityProvider entityProvider = enemy.GetComponent<EntityProvider>();
			    if (entityProvider != null && World.Default.GetStash<BuffStunComponent>().Has(entityProvider.Entity))
			        damage *= (double)(owner.BonusDamageToStunned + 1f);
			}
			if (owner.DamagePerConsumedShockOnTarget > 0f)
			{
			    EntityProvider entityProvider = enemy.GetComponent<EntityProvider>();
			    var stash = World.Default.GetStash<ShockComponent>();
			    if (entityProvider != null && stash.Has(entityProvider.Entity))
			    {
			        int consumed = stash.Get(entityProvider.Entity).ConsumedShockCount;
			        if (consumed > 0) damage *= (double)(owner.DamagePerConsumedShockOnTarget * (float)consumed + 1f);
			    }
			}
			double evasion = data.Target.GetStatSpecificValue(StatType.Evasion);
			if (!fromSimulation)
			{
			    if (evasion.Roll() && !data.Undodgeable) { damage = 0.0; result.HasDodged = true; }
			    damage *= GetEventGolemDamageMultiplier(enemy, metaStats);
			    result.Damage = Math.Round(damage);
			    if (result.IsSuperCrit) owner.BlockSuperCritChanceBonus = 0f;
			    owner.NextHitSuperCritChanceBonus = 0f;
			    owner.NextHitSuperCritDamageBonus = 0f;
			    owner.NextHitDamageBonus = 0f;
			}
			else result.Damage = Math.Round(damage * GetEventGolemDamageMultiplier(enemy, metaStats));
			result.Resist = resist;
			return result;
		}

		private static double GetPlayerEventDamageMultiplier(UnitCore player, EnemyUnit enemy)
		{
			// Native ELF VA 0x140bd50 (tail calls EventBalance).
			if (enemy.Config.IsEvent)
			{
			    EventModel current = GameContext.Get<EventsRepository>().CurrentEvent;
			    if (current != null && enemy.Config.EventDef?.Key == current.Key) return EventBalance.GetPlayerDamageMultiplier();
			}
			return 1.0;
		}

		private static double GetEventGolemDamageMultiplier(EnemyUnit enemy, MetaStatsModel metaStatsModel)
		{
			// Native ELF VA 0x140bef8.
			if (enemy.Config.IsEvent && enemy.Config.EventDef != null && (int)enemy.Config.EventDef.UnitType == 0)
			    return metaStatsModel.Get("event_golem_damage").Value + 1.0;
			return 1.0;
		}

		private static double GetEnemyEventDamageMultiplier(UnitCore player, EnemyUnit enemy)
		{
			// Native ELF VA 0x140c070.
			if (enemy.Config.IsEvent)
			{
			    EventModel current = GameContext.Get<EventsRepository>().CurrentEvent;
			    if (current != null && enemy.Config.EventDef?.Key == current.Key) return EventBalance.GetEnemyDamageMultiplier();
			}
			return 1.0;
		}

		public static double GetGlobalAttackMultiplier(UnitCore source)
		{
			return source.GetStatSpecificValue(StatType.GlobalAttack, true) * 0.01 + 1.0;
		}

		public static double GetGlobalDefenceMultiplier(UnitCore target)
		{
			return 1.0 / (target.GetStatSpecificValue(StatType.GlobalDefence, true) * 0.01 + 1.0);
		}

		private static bool IsIslandMasteryEligible(EnemyUnit enemy)
		{
			// Native ELF VA 0x140c264; bounded ARM64 includes the tail after GetDefaultWorld.
			var config = enemy.Config;
			if (config.IsEvent || config.IsVip || config.IsSpecial || config.IsDummy || config.IsBossFightPart || enemy is CrystalUnit) return false;
			EntityProvider provider = enemy.GetComponent<EntityProvider>();
			return provider == null || !World.Default.GetStash<QuestMobTag>().Has(provider.Entity);
		}

		private static double GetIslandMasteryOutgoingMultiplier(EnemyUnit enemy, MetaStatsModel metaStatsModel)
		{
			// Native ELF VA 0x140be68.
			return IsIslandMasteryEligible(enemy) ? metaStatsModel.Get("island_medal_bonus").GetValue(enemy.Config.LocationNumber) + 1.0 : 1.0;
		}

		private static double GetIslandMasteryIncomingMultiplier(EnemyUnit enemy, MetaStatsModel metaStatsModel)
		{
			// Native ELF VA 0x140c150; original tail math is absent in decompiler return types.
			return IsIslandMasteryEligible(enemy) ? Math.Max(0.0, 1.0 - metaStatsModel.Get("island_medal_bonus").GetValue(enemy.Config.LocationNumber)) : 1.0;
		}

		public static StatType WeaponTypeToDefence(WeaponType type, bool percent)
		{
			switch (type)
			{
				case WeaponType.Stabbing: return percent ? StatType.StabDefencePercent : StatType.StabDefenceFlat;
				case WeaponType.Cutting: return percent ? StatType.CutDefencePercent : StatType.CutDefenceFlat;
				case WeaponType.Magic: return percent ? StatType.MagicDefencePercent : StatType.MagicDefenceFlat;
				case WeaponType.Buckshot: return percent ? StatType.BuckshotDefencePercent : StatType.BuckshotDefenceFlat;
				case WeaponType.Universal: return percent ? StatType.UniversalDefencePercent : StatType.UniversalDefenceFlat;
				default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}
	}
}
