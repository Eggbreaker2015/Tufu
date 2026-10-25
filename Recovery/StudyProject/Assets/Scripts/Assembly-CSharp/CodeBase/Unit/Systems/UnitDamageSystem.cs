using Lofelt.NiceVibrations;
using CodeBase.Audio;
using Codebase.Vibration.Events;
using Codebase.Crystals.Objective;
using Codebase.Enemy;
using Codebase.Abilities.Effects;
using Codebase.Abilities;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using Codebase.Abilities.Events;
using Codebase.Abilities.Systems;
using Codebase.Abilities.Vfx;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.DebugTools.Dummy;
using Codebase.Meta;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Services.Damage;
using Codebase.UI.Damage;
using Codebase.UI.World;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Scellecs.Morpeh;

namespace Codebase.Unit.Systems
{
	public class UnitDamageSystem : SystemBase
	{
		[Inject]
		private UnitDamageViewSystem unitDamageViewSystem;

		[Inject]
		private EnemyDamageEventsViewSystem enemyDamageEventsViewSystem;

		[Inject]
		private PlayerDamageEventsViewSystem playerDamageEventsViewSystem;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private AbilityVfxService abilityVfxService;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private Stash<DummyTargetTag> dummyStash;

		[Inject]
		private Stash<UnitWaitingAttackMarker> waitingMarkerStash;

		[Inject]
		private Stash<UnitFirstVulnerabilityComponent> firstVulnerabilityStash;

		[Inject]
		private Stash<HydraBossComponent> hydraStash;

		[Inject]
		private Stash<MarkComponent> markStash;

		[Inject]
		private Stash<UnitDeadMarker> deadStash;

		[Inject]
		private AbilityExecuteEventSystem abilityExecuteEventSystem;

		private Event<WeaponHitEvent> hitEvent;

		private Event<AbilityTriggerEvent> abilityTriggerEvent;

		private Event<PlayerDeathEvent> playerDeathEvent;

		private Event<UnitDiedEvent> unitDiedEvent;

		public override void OnAwake()
		{
			// Native ELF VA 0x15cd508.
			hitEvent = World.GetEvent<WeaponHitEvent>();
			abilityTriggerEvent = World.GetEvent<AbilityTriggerEvent>();
			playerDeathEvent = World.GetEvent<PlayerDeathEvent>();
			unitDiedEvent = World.GetEvent<UnitDiedEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x15cd600.
			foreach (WeaponHitEvent hit in hitEvent.BatchedChanges) ApplyDamage(hit);
		}

		public void ProcessHitImmediate(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cd810.
			ApplyDamage(data);
		}

		private void ApplyDamage(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cd754.
			if (data.Target is EnemyUnit) ApplyDamageToEnemy(data);
			else ApplyDamageToPlayer(data);
		}

		private void ApplyDamageToPlayer(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cdf10.
			if (data.Target.IsDying) return;
			DamageResult result = DamageCalculator.Calculate(data);
			if (data.HasFixedDamage) result.Damage = data.FixedDamage;
			double damage = cheatsModel.GodMode ? 0.0 : result.Damage;
			EnemyUnit sourceEnemy = data.Source as EnemyUnit;
			if (result.HasBlocked)
			{
			    float bonus = (float)metaStatsModel.Get("block_to_supercrit").Value;
			    if (bonus > 0f) playerModel.BlockSuperCritChanceBonus += bonus;
			    abilityExecuteEventSystem.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnBlock, Target = sourceEnemy });
			}
			if (playerModel.ThornsDamage > 0f) ApplyThornsReflection(data);
			if (result.HasDodged)
			{
			    data.Target.LastDamageGet = Time.time;
			    abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnEvasion });
			    abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnEvasionProc, Target = sourceEnemy });
			    int marks = (int)metaStatsModel.Get("evasion_mark").Value;
			    if (marks > 0 && sourceEnemy != null && HasEquippedPierceWeapon())
			        ApplyMarkEffect.ApplyMark(markStash, World, sourceEnemy, playerModel, marks, false);
			}
			result.Damage = damage;
			if (damage <= 0.0)
			{
			    enemyDamageEventsViewSystem.Create(result);
			    return;
			}
			playerModel.HitsTakenCount++;
			playerModel.HitsTakenLastUpdate = Time.time;
			data.Target.CurrentHealth -= damage;
			data.Target.LastDamageGet = Time.time;
			ApplyLifesteal(data.Source);
			if (data.Target.IsDead && TryIgnoreLethalDamage(data)) data.Target.CurrentHealth = 1.0;
			else enemyDamageEventsViewSystem.Create(result);
			if (data.Target.CurrentHealth <= 0.01 && !data.Target.IsDying)
			{
			    ProcessPlayerDeath(data.Source.name, (PlayerUnit)data.Target);
			    return;
			}
			abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnGetHitLowHealth, Target = sourceEnemy, Damage = damage });
		}

		private void ApplyLifesteal(UnitCore unit)
		{
			// Native ELF VA 0x15ce7e8; this general helper intentionally does not cap health.
			double flat = unit.GetStatValue(StatType.LifestealFlat);
			double percent = unit.GetStatValue(StatType.LifestealPercent);
			if (flat > 0.0 && !unit.IsDead) unit.CurrentHealth += flat;
			if (percent > 0.0 && !unit.IsDead) unit.CurrentHealth += percent * unit.GetStatValue(StatType.Health);
		}

		public void ProcessPlayerDeath(string sourceName, PlayerUnit playerUnit)
		{
			// Native ELF VA 0x15ceb94.
			if (playerUnit.CurrentHealth < 0.0) playerUnit.CurrentHealth = 0.0;
			playerUnit.IsDying = true;
			Scellecs.Morpeh.World.Default.GetRequest<HapticRequest>().Publish(new HapticRequest { preset = (HapticPatterns.PresetType)3 }, true);
			deadStash.Set(playerUnit.EntityProvider.Entity);
			if (!playerUnit.IsDeathAnimationActive) playerUnit.StartDeathAnimation(playerModel).Forget();
			playerUnit.KillerName = sourceName;
			playerDeathEvent.NextFrame(new PlayerDeathEvent { KillerName = sourceName });
		}

		private bool TryIgnoreLethalDamage(WeaponHitEvent data)
		{
			// Native ELF VA 0x15ce89c.
			if (playerModel.LethalIgnoreChance <= 0f && !playerModel.HasPerTargetLethalIgnore) return false;
			if (playerModel.HasPerTargetLethalIgnore)
			{
			    // Unity 6000.6 removes the old GetInstanceID API; retain its per-object key contract.
			    int id = data.Source.GetEntityId().GetHashCode();
			    if (playerModel.LethalIgnoreTargetIds.Contains(id) || playerModel.LethalIgnoreCooldown > 0f) return false;
			    playerModel.LethalIgnoreTargetIds.Add(id);
			}
			else if (UnityEngine.Random.value >= playerModel.LethalIgnoreChance) return false;
			abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnLethalDamageIgnore, Target = data.Source as EnemyUnit });
			playerModel.HasPerTargetLethalIgnore = false;
			playerModel.LethalIgnoreChance = 0f;
			PlayLethalIgnoreFeedback();
			return true;
		}

		private void PlayLethalIgnoreFeedback()
		{
			// Native ELF VA 0x15ced5c.
			abilityVfxService.Spawn(AbilityVfxConstants.DeathEvasion, playerModel.Unit);
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("lethal_ignore");
			enemyDamageEventsViewSystem.CreateLethalIgnore();
		}

		public static void ProcessEnemyDeath(UnitCore enemy, bool wasDeadBefore, Event<UnitDiedEvent> unitDiedEvent)
		{
			// Native ELF VA 0x15ceed8; bounded ARM64 checked past decompiler spill.
			if (!enemy.IsDead || wasDeadBefore) return;
			if (enemy is EnemyUnit && enemy.TryGetComponent<CreeperMobProvider>(out var creeper) && creeper.ShouldDelayDeath) return;
			enemy.OnDied?.Invoke(enemy);
			unitDiedEvent.NextFrame(new UnitDiedEvent { Unit = enemy, Entity = enemy.EntityProvider.Entity });
		}

		private void ApplyThornsReflection(WeaponHitEvent data)
		{
			// Native ELF VA 0x15ce604.
			double damage = playerModel.ThornsDamage;
			if (playerModel.NextThornsDamageBonus > 0f)
			{
			    float bonus = playerModel.NextThornsDamageBonus;
			    playerModel.NextThornsDamageBonus = 0f;
			    damage *= 1f + bonus;
			}
			bool wasDead = data.Source.IsDead;
			data.Source.CurrentHealth -= damage;
			data.Source.LastDamageGet = Time.time;
			ProcessEnemyDeath(data.Source, wasDead, unitDiedEvent);
			enemyDamageEventsViewSystem.CreateReflection(damage, "#1EBBFE");
			SpawnThornsVfx(data);
		}

		private void SpawnHitVfx(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cf2a0; position and scale verified from ARM64 s0/s1/s2/s3.
			if (!(data.Target is EnemyUnit enemy)) return;
			AbilityVfxData vfx = data.IsSuperCrit ? AbilityVfxConstants.SwordSuperCrit : data.IsCrit ? AbilityVfxConstants.SwordCrit : AbilityVfxConstants.SwordHit;
			Vector3 position = enemy.transform.position;
			if (enemy.HealthBarPoint != null) position.y += (enemy.HealthBarPoint.position.y - enemy.transform.position.y) * 0.5f;
			ParticleSystem instance = abilityVfxService.SpawnAt(vfx, position, enemy.transform.localScale.x);
			if (instance != null && instance.TryGetComponent<AbilityHitVfxBinder>(out var binder)) binder.SetEmpowered(data.IsEmpowered);
		}

		private void SpawnThornsVfx(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cf04c; native directional position multiplier is zero.
			Transform target = data.Target.transform;
			Vector3 direction = data.Source.transform.position - target.position;
			direction.y = 0f;
			float yaw = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction).eulerAngles.y : 0f;
			Vector3 offset = direction.sqrMagnitude > 0.0001f ? direction.normalized * 0f : Vector3.zero;
			abilityVfxService.Spawn(AbilityVfxConstants.ReflectionStrike, target, yaw, offset);
		}

		private void ApplyEnemyReflection(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cf560.
			if (!(data.Target is EnemyUnit enemy) || enemy.ReflectionDamage <= 0.0 || !(data.Source is PlayerUnit player) || cheatsModel.GodMode || player.IsDead) return;
			double damage = enemy.ReflectionDamage;
			player.CurrentHealth -= damage;
			player.LastDamageGet = Time.time;
			playerModel.HitsTakenCount++;
			playerModel.HitsTakenLastUpdate = Time.time;
			if (player.CurrentHealth <= 0.01 && !player.IsDying)
			{
			    SpawnEnemyReflectionVfx(data);
			    ProcessPlayerDeath(enemy.name, player);
			}
			else
			{
			    enemyDamageEventsViewSystem.CreateReflection(damage, "#FF5757");
			    SpawnEnemyReflectionVfx(data);
			}
		}

		private void SpawnEnemyReflectionVfx(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cf75c; scalar constants and vector registers checked in ARM64.
			PlayerUnit player = (PlayerUnit)data.Source;
			EnemyUnit enemy = (EnemyUnit)data.Target;
			Vector3 position = enemy.transform.position + Vector3.up * (enemy is CrystalUnit ? 9f : 1.5f);
			Transform target = player.VisualRoot != null ? player.VisualRoot : player.transform;
			Vector3 direction = target.position - position;
			if (direction.sqrMagnitude <= 0.0001f) direction = target.position - enemy.transform.position;
			Quaternion rotation = direction.sqrMagnitude <= 0.0001f ? enemy.transform.rotation : Quaternion.LookRotation(direction);
			abilityVfxService.SpawnAt(AbilityVfxConstants.ReflectionStrike, position, rotation, 1.6f);
		}

		private void ApplyDamageToEnemy(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cd840; original outgoing path does not apply HasFixedDamage.
			EnemyUnit enemy = (EnemyUnit)data.Target;
			abilityExecuteEventSystem.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnPreHit, Target = enemy, Item = data.Weapon });
			DamageResult result = DamageCalculator.Calculate(data);
			if (cheatsModel.OneHitMode) result.Damage = data.Target.GetStatValue(StatType.Health) * 0.9;
			double damage = cheatsModel.OneDamageMode ? 1.0 : result.Damage;
			if (!result.HasDodged)
			{
			    PlayPlayerWeaponImpactSound(data);
			    data.Target.ApplyHitAnimation();
			}
			Entity entity = data.Target.EntityProvider.Entity;
			waitingMarkerStash.Remove(entity);
			if (firstVulnerabilityStash.Has(entity))
			{
			    damage *= firstVulnerabilityStash.Get(entity).DamageModifier;
			    firstVulnerabilityStash.Remove(entity);
			    result.IsEmpowered = true;
			}
			ref DummyTargetTag dummy = ref dummyStash.Get(entity, out bool isDummy);
			data.IsCrit = result.IsCrit;
			data.IsSuperCrit = result.IsSuperCrit;
			data.IsEmpowered = result.IsEmpowered;
			if (!result.HasDodged) SpawnHitVfx(data);
			result.Damage = damage;
			FireHitTriggerEvents(data, result);
			if (isDummy)
			{
			    dummy.Data.RecordHit(damage);
			    data.Target.CurrentHealth = data.Target.GetStatValue(StatType.Health);
			    ApplyPlayerLifesteal(data, entity);
			    playerDamageEventsViewSystem.Create(data, result);
			    return;
			}
			double maxHealth = data.Target.GetStatValue(StatType.Health);
			double oldHealth = data.Target.CurrentHealth;
			bool wasDead = data.Target.IsDead;
			data.Target.CurrentHealth -= damage;
			data.Target.LastDamageGet = Time.time;
			data.Source.LastDamageDealt = Time.time;
			ProcessEnemyDeath(data.Target, wasDead, unitDiedEvent);
			if (!result.HasDodged) ApplyEnemyReflection(data);
			if (data.Target.IsDead && !wasDead && oldHealth >= maxHealth - 0.0001)
			    abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnOneshot, Target = enemy, Damage = damage, Item = data.Weapon });
			ApplyPlayerLifesteal(data, entity);
			playerDamageEventsViewSystem.Create(data, result);
		}

		private void ApplyPlayerLifesteal(WeaponHitEvent data, Entity targetEntity)
		{
			// Native ELF VA 0x15d00cc.
			double flat = data.Source.GetStatValue(StatType.LifestealFlat);
			double percent = data.Source.GetStatValue(StatType.LifestealPercent);
			if (markStash.Has(targetEntity))
			{
			    int stacks = markStash.Get(targetEntity).Stacks;
			    if (stacks > 0)
			    {
			        double bonus = metaStatsModel.Get("mark_lifesteal").Value;
			        if (bonus > 0.0)
			        {
			            double multiplier = 1.0 + bonus * stacks;
			            if (flat > 0.0) flat *= multiplier;
			            if (percent > 0.0) percent *= multiplier;
			        }
			    }
			}
			if (flat > 0.0 && hydraStash.Has(targetEntity) && markStash.Has(targetEntity) && markStash.Get(targetEntity).Stacks > 0)
			    flat *= 1.0 + markStash.Get(targetEntity).Stacks * 0.1;
			if (playerModel.NextHitLifestealFlatBonus > 0f)
			{
			    float bonus = playerModel.NextHitLifestealFlatBonus;
			    playerModel.NextHitLifestealFlatBonus = 0f;
			    flat *= 1f + bonus;
			}
			if (flat > 0.0 && !data.Source.IsDead) data.Source.CurrentHealth += flat;
			if (percent > 0.0 && !data.Source.IsDead) data.Source.CurrentHealth += percent * data.Source.GetStatValue(StatType.Health);
			if (flat > 0.0 || percent > 0.0) data.Source.CurrentHealth = Math.Min(data.Source.CurrentHealth, data.Source.GetStatValue(StatType.Health));
		}

		private void FireHitTriggerEvents(WeaponHitEvent data, DamageResult result)
		{
			// Native ELF VA 0x15cfc64.
			EnemyUnit enemy = data.Target as EnemyUnit;
			abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnHit, Target = enemy, Damage = result.Damage, Item = data.Weapon });
			if (data.IsSuperCrit) abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnSuperCrit, Target = enemy, Damage = result.Damage, Item = data.Weapon });
			if (data.IsCrit)
			{
			    abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnCrit, Target = enemy, Damage = result.Damage, Item = data.Weapon });
			    abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnPostCrit, Target = enemy, Damage = result.Damage, Item = data.Weapon });
			}
			abilityTriggerEvent.NextFrame(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnPostHit, Target = enemy, Damage = result.Damage, Item = data.Weapon });
		}

		private bool HasEquippedPierceWeapon()
		{
			// Native ELF VA 0x15ce71c.
			foreach (var weapon in playerModel.EquippedWeapons) if (weapon.Config.DamageType == WeaponType.Stabbing) return true;
			return false;
		}

		private static void PlayPlayerWeaponImpactSound(WeaponHitEvent data)
		{
			// Native ELF VA 0x15cfab4; four switch strings resolved through ELF relocations.
			string sound;
			switch (data.DamageType)
			{
			    case WeaponType.Stabbing: sound = "weapon_pierce_impact"; break;
			    case WeaponType.Cutting: sound = "weapon_cut_impact"; break;
			    case WeaponType.Magic: sound = "player_hit"; break;
			    case WeaponType.Buckshot: sound = "weapon_smash_impact"; break;
			    default: sound = "player_hit"; break;
			}
			if (data.Target is EnemyUnit enemy && enemy.IsStatic) sound = "weapon_impact_tree";
			GameContext.Get<IAudioPlayer>().PlaySoundOnce(sound);
		}
	}
}
