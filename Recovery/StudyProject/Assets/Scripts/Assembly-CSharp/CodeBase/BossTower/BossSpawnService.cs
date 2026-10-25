using System.Collections.Generic;
using Codebase.Abilities.Systems;
using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Simulation;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Utilities.ObjectPool;
using Scellecs.Morpeh;
using UnityEngine;
using System;
using System.Linq;
using Codebase.Abilities;
using Codebase.Abilities.Events;
using Scellecs.Morpeh.Providers;
using UniMob;
using Codebase.BossTower.Helpers;

namespace Codebase.BossTower
{
	public class BossSpawnService
	{
		[Inject]
		private BossTowerConfig config;
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private EnemiesModel enemiesModel;
		[Inject]
		private AbilityExecuteEventSystem abilityExecuteEventSystem;
		[Inject]
		private SimulationConfig simulationConfig;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private BoneEffectSlotsSystem boneEffectSlotsSystem;
		private World world;
		private Stash<BossTowerBossTag> bossTagStash;
		private Stash<BossVisualComponent> bossVisualtash;
		private Stash<UnitDeadMarker> unitdeadMarkerStash;
		private Stash<BossArenaUnitTag> arenaUnitStash;
		private Stash<UnitComponent> unitStash;
		private Stash<IgnoreFarMarker> ignoreFarStash;
		private Stash<UnitWaitingAttackMarker> waitingMarkerStash;
		private Stash<UnitHealthSyncComponent> healthSyncStash;
		private Stash<UnitWithoutRegenMarker> unitWithoutRegenStash;
		private Stash<UnitHasDeathAnimMarker> deathAnimMarkerStash;
		private Stash<UnitHasSpawnAnimComponent> spawnAnimMarkerStash;
		private Stash<GhostOriginComponent> ghostOriginStash;
		private Stash<ScaleComponent> scaleStash;
		private Stash<BoneEffectSlotsComponent> boneEffectSlotsStash;
		private ObjectPool<EnemyUnit> bossPool;
		private Stash<MarkComponent> markerStash;
		private int hydraChildSpawnCounter;
		public EnemyUnit SpawnBoss(string bossKey, Vector3 position, bool isElite)
		{
			BossDef def = config.Get(bossKey);
			if (def == null)
			{
				Debug.LogError("Boss with key '" + bossKey + "' not found");
				return null;
			}

			return SpawnBoss(def, position, isElite);
		}

		public EnemyUnit SpawnBoss(BossModel bossModel, Vector3 position)
		{
			BossDef def = bossModel.Def;
			if (def.Prefab == null)
			{
				Debug.LogError("Boss '" + def.Key + "' has no prefab");
				return null;
			}

			var spawner = CreateSpawnerConfig(def, bossModel.IsSuperBoss);
			BakeStatsForFloor(spawner, bossModel.GetOptimumTime(), bossModel.IsWeakened);
			return SpawnBossFromConfig(def, spawner, position);
		}

		public EnemyUnit SpawnBoss(BossDef bossDef, Vector3 position, bool isElite)
		{
			if (bossDef.Prefab == null)
			{
				Debug.LogError("Boss '" + bossDef.Key + "' has no prefab");
				return null;
			}

			var spawner = CreateSpawnerConfig(bossDef, isElite);
			return SpawnBossFromConfig(bossDef, spawner, position);
		}

		public void DespawnArena()
		{
			var units = world.GetStash<UnitComponent>();
			var filter = world.Filter.With<BossArenaUnitTag>().Without<UnitDeadMarker>().Build();
			foreach (Entity entity in filter)
			{
				var enemy = (EnemyUnit)units.Get(entity).Core;
				if (boneEffectSlotsStash.Has(entity))
					boneEffectSlotsSystem.ClearAllEffects(boneEffectSlotsStash.Get(entity).Avatar);
				enemy.OnDied -= HandleBossDeath;
				enemy.OnDevoured -= HandleBossDevour;
				enemiesModel.RemoveEnemy(enemy);
				deathAnimMarkerStash.Remove(entity);
				arenaUnitStash.Remove(entity);
				bossPool.Despawn(enemy);
				enemy.CurrentHealth = 0;
			}
		}

		public EnemyUnit SpawnGhostViking(EnemyUnit deadViking, VikingsBossComponent vikingData)
		{
			var(instance, entity) = SpawnUnit(vikingData.GhostPrefab, deadViking.transform.position, deadViking.transform.rotation, deadViking.Config);
			world.GetStash<VikingsBossComponent>().Set(entity, new VikingsBossComponent { GroupId = vikingData.GroupId, BossKey = vikingData.BossKey, BuffPerDeadAlly = vikingData.BuffPerDeadAlly, SpawnerConfig = deadViking.Config, GhostPrefab = null, IsGhost = true, Index = vikingData.Index, BuffVfxPrefab = vikingData.BuffVfxPrefab, BuffTargetVfxPrefab = vikingData.BuffTargetVfxPrefab, BuffForceFieldPrefab = vikingData.BuffForceFieldPrefab, HandEffectPrefab = vikingData.HandEffectPrefab, DebuffPerRageLevel = vikingData.DebuffPerRageLevel, DebuffDuration = vikingData.DebuffDuration, ScaleStep = vikingData.ScaleStep });
			bossTagStash.Set(entity, new BossTowerBossTag { UnitGuid = string.Format("viking_{0}_ghost_{1}", vikingData.GroupId, vikingData.Index), BossFightKey = vikingData.BossKey });
			deathAnimMarkerStash.Set(entity);
			spawnAnimMarkerStash.Set(entity, new UnitHasSpawnAnimComponent { TriggerHash = Animator.StringToHash("Spawn") });
			arenaUnitStash.Set(entity);
			ghostOriginStash.Set(entity, new GhostOriginComponent { OriginalUnitGuid = string.Format("viking_{0}_{1}", vikingData.GroupId, vikingData.Index) });
			healthSyncStash.Set(entity, new UnitHealthSyncComponent { PrevMaxHp = instance.GetStatValue(StatType.Health, true) });
			scaleStash.Set(entity, new ScaleComponent { OriginalScale = instance.OriginalScale, keepScaleAfterDeath = true });
			instance.EnsureScaleStat();
			instance.transform.localScale = deadViking.transform.localScale;
			return instance;
		}

		private EnemyUnit SpawnBossFromConfig(BossDef bossDef, EnemySpawnerConfig spawnerConfig, Vector3 position)
		{
			var(instance, entity) = SpawnUnit(bossDef.Prefab, position, Quaternion.identity, spawnerConfig);
			bossTagStash.Set(entity, new BossTowerBossTag { UnitGuid = bossDef.Key, BossFightKey = bossDef.Key });
			arenaUnitStash.Set(entity);
			waitingMarkerStash.Set(entity);
			deathAnimMarkerStash.Set(entity);
			ApplyBehaviors(entity, bossDef, instance, spawnerConfig);
			healthSyncStash.Set(entity, new UnitHealthSyncComponent { PrevMaxHp = instance.GetStatValue(StatType.Health, true) });
			return instance;
		}

		private (EnemyUnit, Entity) SpawnUnit(EnemyUnit prefab, Vector3 position, Quaternion rotation, EnemySpawnerConfig config, List<Stat> stats = null)
		{
			var instance = bossPool.Spawn(prefab, position, rotation, null);
			Entity entity = instance.GetComponent<EntityProvider>().Entity;
			ClearStaleState(entity);
			instance.transform.localScale = prefab.transform.localScale;
			instance.SetDeathState(false);
			instance.Initialize(Lifetime.Eternal, config, position, stats);
			enemiesModel.AddEnemy(instance);
			instance.OnDied += HandleBossDeath;
			instance.OnDevoured += HandleBossDevour;
			if (markerStash.Has(entity))
				markerStash.Remove(entity);
			ignoreFarStash.Set(entity);
			unitWithoutRegenStash.Set(entity);
			if (bossVisualtash.Has(entity))
				bossVisualtash.Get(entity).Visual.SetActive(true);
			return (instance, entity);
		}

		private void ClearStaleState(Entity entity)
		{
			if (unitdeadMarkerStash.Has(entity))
				unitdeadMarkerStash.Remove(entity);
		}

		private void HandleBossDeath(UnitCore unit)
		{
			unit.OnDied -= HandleBossDeath;
			var enemy = (EnemyUnit)unit;
			Entity entity = enemy.EntityProvider.Entity;
			if (boneEffectSlotsStash.Has(entity))
				boneEffectSlotsSystem.ClearAllEffects(boneEffectSlotsStash.Get(entity).Avatar);
			abilityExecuteEventSystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnKill, Target = enemy });
			enemiesModel.RemoveEnemy(enemy);
			if (!deathAnimMarkerStash.Has(entity))
				EnemySpawner.OnDied?.Invoke(enemy);
		}

		private void HandleBossDevour(UnitCore unit)
		{
			var enemy = (EnemyUnit)unit;
			Entity entity = enemy.EntityProvider.Entity;
			abilityExecuteEventSystem.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnDevour, Target = enemy });
			enemy.OnDevoured -= HandleBossDevour;
			arenaUnitStash.Remove(entity);
			bossPool.Despawn(enemy);
		}

		public EnemySpawnerConfig CreateSpawnerConfig(BossDef bossDef, bool isElite)
		{
			return CreateBossFightConfig(bossDef.Key, bossDef.Prefab, bossDef.Stats, bossDef.WeaponType, bossDef.ArmorConfig, bossDef.AggroRange, bossDef.AggroDuration, bossDef.IsFlying, isElite ? bossDef.EliteAverageFightTime : bossDef.AverageFightTime, 1, isElite ? bossDef.EliteDifficulty : bossDef.Difficulty);
		}

		public static EnemySpawnerConfig CreateBossFightConfig(string key, EnemyUnit prefab, List<Stat> stats, WeaponType weaponType, ArmorConfig armorConfig, float aggroRange, float aggroDuration, bool isFlying, float averageFightTime, int count, float difficulty)
		{
			return new EnemySpawnerConfig
			{
				Key = key,
				Prefab = prefab,
				Stats = stats,
				WeaponType = weaponType,
				ArmorConfig = armorConfig,
				AggroRange = aggroRange,
				AggroDuration = aggroDuration,
				IsFlying = isFlying,
				IsStatic = false,
				IsEvent = false,
				SpawnArea = Vector2.zero,
				IsBossFightPart = true,
				AverageFightTime = averageFightTime,
				Count = count,
				Difficulty = difficulty
			};
		}

		public EnemyUnit SpawnHydraChild(string key, EnemyUnit prefab, Vector3 position, EnemySpawnerConfig config, HydraBossComponent hydraData, Vector3 scale, int index, List<Stat> stats = null, int startStacks = 0)
		{
			// Native ELF VA 0x1555fdc; original unique child identity, timer and inherited marks.
			var(instance, entity) = SpawnUnit(prefab, position, Quaternion.identity, config, stats);
			instance.transform.localScale = scale;
			arenaUnitStash.Set(entity);
			world.GetStash<HydraBossComponent>().Set(entity, hydraData);
			healthSyncStash.Set(entity, new UnitHealthSyncComponent { PrevMaxHp = instance.GetStatValue(StatType.Health, true) });
			hydraChildSpawnCounter = unchecked(hydraChildSpawnCounter + 1);
			bossTagStash.Set(entity, new BossTowerBossTag { UnitGuid = string.Format("hydra_{0}_{1}_{2}", hydraData.RemainingSpawns, index, hydraChildSpawnCounter), BossFightKey = key });
			spawnAnimMarkerStash.Set(entity, new UnitHasSpawnAnimComponent { finishByTimer = true, maxTimer = .9f });
			deathAnimMarkerStash.Set(entity, new UnitHasDeathAnimMarker { instantDespawnAfterAnim = true, deathParticle = hydraData.DeathVfxPrefab });
			if (startStacks > 0)
				global::Codebase.Abilities.Effects.ApplyMarkEffect.ApplyMark(markerStash, world, instance, playerModel, startStacks, true);
			return instance;
		}

		private void ApplyBehaviors(Entity entity, BossDef bossDef, EnemyUnit instance, EnemySpawnerConfig spawnerConfig)
		{
			foreach (var behavior in bossDef.Behaviors)
			{
				if (behavior is HydraBossConfig hydra)
					ApplyHydraBehavior(entity, hydra, instance, spawnerConfig, bossDef);
				else if (behavior is AssassinBossConfig assassin)
					ApplyAssassinBehavior(entity, assassin, bossDef, spawnerConfig);
				else if (behavior is LavaCrabBossConfig crab)
					ApplyLavaCrabBehavior(entity, crab, bossDef.Key, spawnerConfig);
				else if (behavior is FireElementalBossConfig fire)
					ApplyFireElementalBehavior(entity, fire, spawnerConfig);
				else if (behavior is VampireBossConfig vampire)
					ApplyVampireBehavior(entity, vampire, bossDef.Key, spawnerConfig);
				else if (behavior is VikingsBossConfig vikings)
					ApplyVikingsBehavior(entity, vikings, bossDef.Key, instance.transform.position, bossDef, spawnerConfig, instance);
				else if (behavior is LichBossConfig lich)
					ApplyLichBehavior(entity, lich, bossDef.Key, spawnerConfig);
				else if (behavior is MageBossConfig mage)
					ApplyMageBehavior(entity, mage, spawnerConfig);
				else if (behavior is BerserkerBossConfig berserker)
					ApplyBerserkerBehavior(entity, berserker, bossDef.Key, instance);
			}
		}

		private void ApplyHydraBehavior(Entity entity, HydraBossConfig config, EnemyUnit instance, EnemySpawnerConfig spawnerConfig, BossDef bossDef)
		{
			// Native ELF VA 0x1556364; stat selector 0x1559154.
			BossModel boss = bossTowerModel.GetBoss(bossDef.Key);
			int maximumSplits = boss != null && boss.IsSuperBoss ? unchecked(config.MaxSplits + 1) : config.MaxSplits;
			Renderer renderer = bossDef.Prefab.GetComponentInChildren<Renderer>();
			Material originalMaterial = renderer != null ? renderer.sharedMaterial : null;
			var baked = new List<Stat>[maximumSplits];
			for (int generation = 0; generation < maximumSplits; generation++)
			{
				if (generation >= config.GenerationStats.Count)
					continue;
				var stats = new List<Stat>();
				foreach (MinionStat definition in config.GenerationStats[generation].Stats)
				{
					Stat original = spawnerConfig.Stats.FirstOrDefault(stat => stat.Type == definition.Type);
					if (original != null)
						stats.Add(new Stat(Lifetime.Eternal, original.BaseValue * definition.Multiplier, definition.Type));
				}

				baked[generation] = stats;
			}

			world.GetStash<HydraBossComponent>().Set(entity, new HydraBossComponent { RemainingSpawns = maximumSplits, SpawnCount = config.SpawnCount, MaxSplits = maximumSplits, SizeMultiplier = config.SizeMultiplier, CurrentScaleMultiplier = 1f, ChildSpawnRadius = config.ChildSpawnRadius, Prefab = bossDef.Prefab, SplitPrefabs = config.SplitPrefabs.Count > 0 ? config.SplitPrefabs.ToArray() : null, ParentConfig = spawnerConfig, OriginalMaterial = originalMaterial, OriginalScale = bossDef.Prefab.transform.localScale, BakedStatsPerLevel = baked, SpawnProjectilePrefab = config.SpawnProjectilePrefab, SpawnProjectileSpeed = config.SpawnProjectileSpeed, DeathVfxPrefab = config.DeathVfxPrefab, DeathVfxLifetime = config.DeathVfxLifetime });
			deathAnimMarkerStash.Set(entity, new UnitHasDeathAnimMarker { instantDespawnAfterAnim = true, deathParticle = config.DeathVfxPrefab });
			ApplyHydraSpawnMarks(instance);
		}

		private void ApplyHydraSpawnMarks(EnemyUnit instance)
		{
			// Native ELF VA 0x155829c; original literal boss_tower_hydra.
			double value = metaStatsModel.Get("boss_tower_hydra").Value;
			int stacks = double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= 2147483648.0 ? int.MaxValue : value <= -2147483648.0 ? int.MinValue : (int)value;
			if (stacks > 0)
				global::Codebase.Abilities.Effects.ApplyMarkEffect.ApplyMark(markerStash, world, instance, playerModel, stacks, true);
		}

		private void ApplyAssassinBehavior(Entity entity, AssassinBossConfig config, BossDef bossDef, EnemySpawnerConfig spawnerConfig)
		{
		}

		private void ApplyLavaCrabBehavior(Entity entity, LavaCrabBossConfig config, string bossKey, EnemySpawnerConfig spawnerConfig)
		{
			BossModel boss = bossTowerModel.GetBoss(bossKey);
			bool elite = boss != null && boss.IsSuperBoss;
			int hits = (elite ? 7 : config.HitsToExplode) + (int)metaStatsModel.Get("boss_tower_crab").Value;
			world.GetStash<LavaCrabBossComponent>().Set(entity, new LavaCrabBossComponent { HitsToExplode = hits, ExplosionDelay = config.ExplosionDelay, DamageMultiplier = config.DamageMultiplier, HealOnExplosion = elite, HealPercent = config.HealPercent, ExplosionVfxPrefab = config.ExplosionVfxPrefab, ExplosionVfxLifetime = config.ExplosionVfxLifetime, SpawnerConfig = spawnerConfig });
			world.GetStash<LightningVulnerabilityComponent>().Set(entity, new LightningVulnerabilityComponent { DamageMultiplier = 1.25f });
		}

		private void ApplyFireElementalBehavior(Entity entity, FireElementalBossConfig config, EnemySpawnerConfig spawnerConfig)
		{
		}

		private void ApplyVampireBehavior(Entity entity, VampireBossConfig config, string bossKey, EnemySpawnerConfig spawnerConfig)
		{
			BossModel boss = bossTowerModel.GetBoss(bossKey);
			bool elite = boss != null && boss.IsSuperBoss;
			int count = elite ? unchecked(config.SpiritCount + 2) : config.SpiritCount;
			var baked = new List<Stat>();
			foreach (MinionStat minion in config.SpiritStats)
			{
				Stat original = spawnerConfig.Stats.FirstOrDefault(stat => stat.Type == minion.Type);
				if (original != null)
					baked.Add(new Stat(Lifetime.Eternal, original.BaseValue * minion.Multiplier, minion.Type));
			}

			float interval = (float)((metaStatsModel.Get("boss_tower_vampire").Value + 1.0) * config.SpiritInterval);
			world.GetStash<VampireBossComponent>().Set(entity, new VampireBossComponent { SpiritInterval = interval, SpiritTimer = 4f, SpiritCount = count, HealPercent = config.HealPercent, DamageBoostPercent = config.DamageBoostPercent, ArmorBoostPercent = config.ArmorBoostPercent, SpeedBoostPerSpirit = config.SpeedBoostPerSpirit, SpiritReachDistance = config.SpiritReachDistance, ArmorBoostOnSpiritReach = elite, SpiritPrefab = config.SpiritPrefab, SpiritVfx = config.SpiritVfx, SpiritConfig = config.SpiritConfig, SpawnerConfig = spawnerConfig, SpiritVfxLifetime = config.SpiritVfxLifetime, SpiritSoulVfx = config.SpiritSoulVfx, SpiritSoulForceField = config.SpiritSoulForceField, VampireBuffVfx = config.VampireBuffVfx, SpiritSpawnVfx = config.SpiritSpawnVfx, HandEffectPrefab = config.HandEffectPrefab, BakedSpiritStats = baked });
			if (unitStash.Has(entity))
			{
				var unit = unitStash.Get(entity).Core;
				if (unit.HasStat(StatType.UniversalDefenceFlat))
					unit.RemoveModifier(StatType.UniversalDefenceFlat, "VampireSpiritArmorBuff");
				unit.RemoveModifier(StatType.Damage, "VampireSpiritBuff");
				unit.RemoveModifier(StatType.MovementSpeed, "VampireSpiritSpeedBuff");
			}
		}

		private void ApplyVikingsBehavior(Entity entity, VikingsBossConfig config, string groupKey, Vector3 basePosition, BossDef baseBossDef, EnemySpawnerConfig spawnerConfig, EnemyUnit instance)
		{
			int groupId = groupKey.GetHashCode();
			world.GetStash<VikingsBossComponent>().Set(entity, new VikingsBossComponent { GroupId = groupId, BossKey = groupKey, BuffPerDeadAlly = config.BuffPerDeadAlly, SpawnerConfig = spawnerConfig, GhostPrefab = config.GhostViewPrefab, IsGhost = false, Index = 0, BuffVfxPrefab = config.BuffVfxPrefab, BuffTargetVfxPrefab = config.BuffTargetVfxPrefab, BuffForceFieldPrefab = config.BuffForceFieldPrefab, HandEffectPrefab = config.HandEffectPrefab, DebuffPerRageLevel = config.DebuffPerRageLevel, DebuffDuration = config.DebuffDuration, ScaleStep = config.ScaleStep });
			bool elite = bossTowerModel.GetBoss(baseBossDef.Key).IsSuperBoss;
			scaleStash.Set(entity, new ScaleComponent { OriginalScale = instance.OriginalScale });
			instance.EnsureScaleStat();
			for (int i = 0; i < config.AdditionalVikings.Count; i++)
			{
				VikingVariant variant = config.AdditionalVikings[i];
				SpawnAdditionalViking(variant, i + 1, basePosition, groupId, groupKey, config.BuffPerDeadAlly, config.AggroRange, config.AggroDuration, variant.GhostPrefab, baseBossDef, config.BuffVfxPrefab, config.BuffTargetVfxPrefab, config.BuffForceFieldPrefab, config.HandEffectPrefab, config.DebuffPerRageLevel, config.DebuffDuration, config.ScaleStep, elite);
			}
		}

		private void SpawnAdditionalViking(VikingVariant variant, int index, Vector3 basePosition, int groupId, string bossKey, float buffPerDeadAlly, float aggroRange, float aggroDuration, EnemyUnit ghostPrefab, BossDef baseBossDef, ParticleSystem buffVfxPrefab, ParticleSystem buffTargetVfxPrefab, ParticleSystemForceField buffForceFieldPrefab, ParticleSystem handEffectPrefab, float debuffPerRageLevel, float debuffDuration, float scaleStep, bool isElite)
		{
			if (variant.Prefab == null)
				return;
			Vector3 position = basePosition + variant.SpawnOffset;
			var stats = new List<Stat>();
			foreach (Stat stat in baseBossDef.Stats)
			{
				MinionStat multiplier = MinionStat.Default;
				foreach (MinionStat candidate in variant.MinionStats)
					if (candidate.Type == stat.Type)
					{
						multiplier = candidate;
						break;
					}

				stats.Add(new Stat(Lifetime.Eternal, stat.BaseValue * (double)multiplier.Multiplier, stat.Type));
			}

			EnemySpawnerConfig config = CreateBossFightConfig(string.Format("viking_{0}_{1}", groupId, variant.WeaponType), variant.Prefab, stats, variant.WeaponType, variant.ArmorConfig, aggroRange, aggroDuration, false, isElite ? baseBossDef.EliteAverageFightTime : baseBossDef.AverageFightTime, 1, isElite ? baseBossDef.EliteDifficulty : baseBossDef.Difficulty);
			var(instance, entity) = SpawnUnit(variant.Prefab, position, Quaternion.identity, config);
			instance.transform.rotation = Quaternion.Euler(0f, variant.SpawnRotationY, 0f);
			world.GetStash<VikingsBossComponent>().Set(entity, new VikingsBossComponent { GroupId = groupId, BossKey = bossKey, BuffPerDeadAlly = buffPerDeadAlly, SpawnerConfig = config, GhostPrefab = ghostPrefab, IsGhost = false, Index = index, BuffVfxPrefab = buffVfxPrefab, BuffTargetVfxPrefab = buffTargetVfxPrefab, BuffForceFieldPrefab = buffForceFieldPrefab, HandEffectPrefab = handEffectPrefab, DebuffPerRageLevel = debuffPerRageLevel, DebuffDuration = debuffDuration, ScaleStep = scaleStep });
			deathAnimMarkerStash.Set(entity);
			bossTagStash.Set(entity, new BossTowerBossTag { UnitGuid = string.Format("viking_{0}_{1}", groupId, index), BossFightKey = bossKey });
			arenaUnitStash.Set(entity);
			waitingMarkerStash.Set(entity);
			healthSyncStash.Set(entity, new UnitHealthSyncComponent { PrevMaxHp = instance.GetStatValue(StatType.Health, true) });
			scaleStash.Set(entity, new ScaleComponent { OriginalScale = instance.transform.localScale, keepScaleAfterDeath = true });
			instance.EnsureScaleStat();
		}

		private void ApplyLichBehavior(Entity entity, LichBossConfig config, string bossKey, EnemySpawnerConfig spawnerConfig)
		{
			// Original ELF VA 0x155783c; ordinary bosses stop one stage before elites.
			BossModel boss = bossTowerModel.GetBoss(bossKey);
			bool elite = boss != null && boss.IsSuperBoss;
			var variants = new SkeletonVariant[config.EvolutionStages.Count];
			for (int i = 0; i < config.EvolutionStages.Count; i++)
			{
				SkeletonEvolutionStage stage = config.EvolutionStages[i];
				var baked = new List<Stat>();
				foreach (MinionStat minion in stage.Stats)
				{
					Stat original = spawnerConfig.Stats.FirstOrDefault(stat => stat.Type == minion.Type);
					if (original != null)
						baked.Add(new Stat(Lifetime.Eternal, original.BaseValue * minion.Multiplier, minion.Type));
				}

				variants[i] = new SkeletonVariant
				{
					Prefab = stage.Prefab,
					VfxPrefab = stage.VfxPrefab,
					UpgradeTime = stage.UpgradeTime,
					BakedStats = baked
				};
			}

			float interval = (float)((metaStatsModel.Get("boss_tower_lich").Value + 1.0) * config.SkeletonInterval);
			world.GetStash<LichBossComponent>().Set(entity, new LichBossComponent { SkeletonInterval = interval, SkeletonTimer = interval, SkeletonSpawnRadius = config.SkeletonSpawnRadius, SkeletonSpawnCount = elite ? 2 : 1, SkeletonDeathMarkChance = config.SkeletonDeathMarkChance, SpawnVfxPrefab = config.SpawnVfxPrefab, SpawnVfxLifetime = config.SpawnVfxLifetime, SkeletonConfig = config.SkeletonConfig, EvolutionVariants = variants, MaxEvolutionLevel = elite ? variants.Length : variants.Length - 1 });
			world.GetStash<Codebase.Enemy.Components.EnemyCanEscapeMarker>().Set(entity, new Codebase.Enemy.Components.EnemyCanEscapeMarker { EscapeDistance = config.EscapeDistance, ThreatCooldown = config.EscapeThreatCooldown, TimeSinceLastThreat = 0f, StuckTimerMax = 5f, StuckTimer = 0f });
		}

		private void ApplyBerserkerBehavior(Entity entity, BerserkerBossConfig config, string bossKey, EnemyUnit instance)
		{
			BossModel boss = bossTowerModel.GetBoss(bossKey);
			bool elite = boss != null && boss.IsSuperBoss;
			world.GetStash<BerserkerBossComponent>().Set(entity, new BerserkerBossComponent { CurrentRage = 0f, RageLevel = 0, MaxRageLevel = elite ? 4 : 3, RagePerSecond = elite ? config.RagePerSecond * 1.5f : config.RagePerSecond, CastDuration = config.CastDuration, RageDecreaseOnPlayerLevelUp = config.RageDecreaseOnPlayerLevelUp, RageThresholds = config.RageThresholds, BossKey = bossKey, BonusSpeedPerLevel = config.BonusSpeedPerLevel, BonusAttackRangePercentPerLevel = config.BonusAttackRangePercentPerLevel });
			world.GetStash<ScaleComponent>().Set(entity, new ScaleComponent { OriginalScale = instance.OriginalScale, keepScaleAfterDeath = true });
			instance.EnsureScaleStat();
			if (unitStash.Has(entity))
			{
				UnitCore unit = unitStash.Get(entity).Core;
				unit.RemoveModifier(StatType.VisualScaleMultiplier, "BerserkerRage");
				unit.RemoveModifier(StatType.Damage, "BerserkerRage");
				unit.RemoveModifier(StatType.Health, "BerserkerRage");
				unit.RemoveModifier(StatType.AttackRange, "BerserkerRage");
				unit.RemoveModifier(StatType.MovementSpeed, "BerserkerRage");
			}
		}

		private void ApplyMageBehavior(Entity entity, MageBossConfig config, EnemySpawnerConfig spawnerConfig)
		{
			// Original ELF VA 0x1557ee4.
			world.GetStash<MageBossComponent>().Set(entity, new MageBossComponent { HitsToTeleport = config.HitsToTeleport, CurrentHitCount = 0, RetreatDistance = config.RetreatDistance, RetreatDuration = config.RetreatDuration, HasAttackedThisCycle = false, SpawnerConfig = spawnerConfig });
		}

		public void BakeStatsForFloor(EnemySpawnerConfig spawnerConfig, float optimumTime, bool isWeakened)
		{
			var snapshots = simulationConfig.TimeChunkSnapshots;
			if (snapshots == null || snapshots.Count < 1)
				snapshots = simulationConfig.SimulationData.TimeChunkSnapshots;
			float chunk = optimumTime / simulationConfig.TimeChunkDurationMinutes;
			var snapshot = simulationConfig.SimulationData.GetByTimeChunkIndex(chunk, snapshots);
			var healthStat = spawnerConfig.Stats.FirstOrDefault(stat => stat.Type == StatType.Health);
			var damageStat = spawnerConfig.Stats.FirstOrDefault(stat => stat.Type == StatType.Damage);
			if (healthStat == null || damageStat == null || spawnerConfig.ArmorConfig == null)
				return;
			double critChance = Stat.CalculateSpecificValue(StatType.CritChance, snapshot.Stats.First(stat => stat.Key == StatType.CritChance).TotalValue);
			double critDamage = Stat.CalculateSpecificValue(StatType.CritDamagePercent, snapshot.Stats.First(stat => stat.Key == StatType.CritDamagePercent).TotalValue);
			float damageCoefficient = simulationConfig.CombatBalanceConfig.GetInterpolationCoefficient(chunk, (Codebase.Balance.BalanceInterpolationType)4);
			float defenseCoefficient = simulationConfig.CombatBalanceConfig.GetInterpolationCoefficient(chunk, (Codebase.Balance.BalanceInterpolationType)5);
			Armor armor = spawnerConfig.ArmorConfig.Armors.OrderBy(value => value.Resist).FirstOrDefault();
			double dps = snapshot.DpsData.First(value => value.WeaponType == armor.Type).Value;
			float cappedResist = armor.Resist > 0.9f ? 0.9f : armor.Resist;
			double defense = snapshot.DefenseData.First(value => value.WeaponType == spawnerConfig.WeaponType).Value;
			double healthRegen = snapshot.Stats.First(stat => stat.Key == StatType.HealthRegen).TotalValue;
			double lifesteal = snapshot.Stats.First(stat => stat.Key == StatType.LifestealFlat).TotalValue;
			double evasion = Stat.CalculateSpecificValue(StatType.Evasion, snapshot.Stats.First(stat => stat.Key == StatType.Evasion).TotalValue);
			double blockChance = Stat.CalculateSpecificValue(StatType.BlockChance, snapshot.Stats.First(stat => stat.Key == StatType.BlockChance).TotalValue);
			double blockAmount = Stat.CalculateSpecificValue(StatType.BlockAmount, snapshot.Stats.First(stat => stat.Key == StatType.BlockAmount).TotalValue);
			float attackers = Math.Min(4f, (spawnerConfig.Count + 1) / 2f);
			double difficulty = spawnerConfig.Difficulty > 0f ? spawnerConfig.Difficulty : 1.0;
			double regen = Codebase.Services.Damage.DamageCalculator.GetBattleRegen(healthRegen, lifesteal, difficulty);
			double playerHealth = snapshot.Stats.First(stat => stat.Key == StatType.Health).TotalValue;
			double cooldown = spawnerConfig.Stats.First(stat => stat.Type == StatType.AttackCooldown).BaseValue;
			double damage = Codebase.Services.Damage.DamageCalculator.SolveAttackDamageForIncomingDamage((((regen + difficulty * playerHealth / spawnerConfig.AverageFightTime) / (double)attackers) * (cooldown / (1.0 - evasion))) / (1.0 - blockChance * blockAmount), defense, 1.0 / ((double)defenseCoefficient + 1.0));
			dps = ((double)damageCoefficient + 1.0) * dps;
			float resistanceMultiplier = armor.Resist < -0.9f ? 1.9f : 1f - cappedResist;
			double health = ((((1.0 - critChance) * dps + critDamage * (critChance * dps)) * (double)spawnerConfig.AverageFightTime) / (double)spawnerConfig.Count) * (double)resistanceMultiplier;
			float beyond = chunk - (float)snapshots.Max(value => value.TimeChunkIndex);
			if (beyond > 0f)
			{
				double scale = Math.Pow(1.05, (double)beyond);
				damage *= scale;
				health *= scale;
			}

			damage = Math.Round(damage / 10.0) * 10.0;
			health = Math.Round(health);
			double weakness = (double)((float)metaStatsModel.Get("tower_boss_weakness").Value + 1f);
			if (!isWeakened)
				weakness = 1.0;
			healthStat.BaseValue = health / weakness;
			damageStat.BaseValue = damage / weakness;
		}

		public void BakeStatsForFloor(EnemySpawnerConfig spawnerConfig, float optimumTime, bool isWeakened, float damageMultiplier, float healthMultiplier)
		{
			BakeStatsForFloor(spawnerConfig, optimumTime, isWeakened);
			spawnerConfig.Stats.First(stat => stat.Type == StatType.Damage).BaseValue *= (double)damageMultiplier;
			spawnerConfig.Stats.First(stat => stat.Type == StatType.Health).BaseValue *= (double)healthMultiplier;
		}

		public BossSpawnService()
		{
			bossPool = new ObjectPool<EnemyUnit>();
			world = World.Default;
			bossTagStash = world.GetStash<BossTowerBossTag>();
			ignoreFarStash = world.GetStash<IgnoreFarMarker>();
			arenaUnitStash = world.GetStash<BossArenaUnitTag>();
			waitingMarkerStash = world.GetStash<UnitWaitingAttackMarker>();
			healthSyncStash = world.GetStash<UnitHealthSyncComponent>();
			unitWithoutRegenStash = world.GetStash<UnitWithoutRegenMarker>();
			unitStash = world.GetStash<UnitComponent>();
			deathAnimMarkerStash = world.GetStash<UnitHasDeathAnimMarker>();
			spawnAnimMarkerStash = world.GetStash<UnitHasSpawnAnimComponent>();
			ghostOriginStash = world.GetStash<GhostOriginComponent>();
			scaleStash = world.GetStash<ScaleComponent>();
			boneEffectSlotsStash = world.GetStash<BoneEffectSlotsComponent>();
			markerStash = world.GetStash<MarkComponent>();
			unitdeadMarkerStash = world.GetStash<UnitDeadMarker>();
			bossVisualtash = world.GetStash<BossVisualComponent>();
		}
	}
}
