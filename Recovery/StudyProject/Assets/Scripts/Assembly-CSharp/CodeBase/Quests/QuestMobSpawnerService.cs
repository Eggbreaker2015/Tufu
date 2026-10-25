using System.Collections.Generic;
using Codebase.ABTests;
using Codebase.Abilities.Systems;
using Codebase.Analytics;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Location;
using Codebase.Map.Components;
using Codebase.Player.Models;
using Codebase.Quests.Components;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Utilities.ObjectPool;
using Scellecs.Morpeh;
using UnityEngine;
using System.Linq;
using UniMob;
using UnityEngine.AI;
using Scellecs.Morpeh.Providers;
using Codebase.Abilities;
using Codebase.Abilities.Events;
using Codebase.Quests.Commands;

namespace Codebase.Quests
{
	public class QuestMobSpawnerService
	{
		private class ActiveQuestMob
		{
			public QuestArtificialSpawner Spawner;
			public EnemyUnit Unit;
			public EnemySpawner ReferenceSpawner;
		}

		[Inject]
		private EnemiesModel enemiesModel;
		[Inject]
		private BossSpawnService bossSpawnService;
		[Inject]
		private AbilityExecuteEventSystem abilityExecuteEventSystem;
		[Inject]
		private UserModel userModel;
		[Inject]
		private LocationsConfig locationsConfig;
		[Inject]
		private ABTestService abTestService;
		[Inject]
		private QuestConfig config;
		[Inject]
		private PlayerModel playerModel;
		private readonly Dictionary<string, ActiveQuestMob> activeByStep;
		private readonly Dictionary<int, EnemySpawner> lastSpawnerByLocation;
		private readonly ObjectPool<EnemyUnit> unitPool;
		private readonly Stash<QuestMobTag> mobTagStash;
		private readonly Stash<UnitDeadMarker> deadMarkerStash;
		private readonly Stash<IgnoreFarMarker> ignoreFarStash;
		private readonly Stash<DynamicPoiTag> dynamicPoiStash;
		public void DespawnAll()
		{ // Original ELF 0x140fdd4; independent candidate only.
			foreach (var active in activeByStep.Values.ToList())
				DespawnUnit(active);
		}

		public void SyncStep(string stepKey, QuestStepDef stepDef, bool shouldSpawn, bool focusCamera = false)
		{ // Original ELF 0x1410c64; independent candidate only.
			if (stepDef.Prefab == null || !shouldSpawn)
				return;
			if (activeByStep.TryGetValue(stepKey, out var active))
			{
				if (active.Unit != null && !active.Unit.IsDead)
					return;
				activeByStep.Remove(stepKey);
			}

			Spawn(stepKey, stepDef, focusCamera);
		}

		private void Spawn(string stepKey, QuestStepDef stepDef, bool focusCamera)
		{ // Original ELF 0x1424700; independent candidate only.
			var referenceSpawner = PickSpawner(stepDef.LocationNumber);
			if (referenceSpawner == null)
				return;
			var chain = config.GetChainOfStep(stepKey);
			float fightTime = GetFightTime(stepKey, chain);
			var spawnerConfig = BuildConfig(stepDef, fightTime, chain, referenceSpawner);
			if (!TryPickSpawnPosition(referenceSpawner.transform.position, spawnerConfig, out var position))
				return;
			var artificial = new QuestArtificialSpawner
			{
				Config = spawnerConfig,
				Position = position
			};
			float optimum = stepDef.IsDays ? SpawnerOptimumTimeDaysConverter.ConvertDaysToOptimum(stepDef.OptimumTime) : stepDef.OptimumTime;
			bossSpawnService.BakeStatsForFloor(spawnerConfig, optimum, false, chain.MobAttackMultiplier, chain.MobHealthMultiplier);
			var unit = SpawnUnit(artificial, stepKey, focusCamera, chain);
			if (unit == null)
				return;
			activeByStep[stepKey] = new ActiveQuestMob
			{
				Spawner = artificial,
				Unit = unit,
				ReferenceSpawner = referenceSpawner
			};
		}

		private EnemyUnit SpawnUnit(QuestArtificialSpawner spawner, string stepKey, bool focusCamera, QuestChainDef chain)
		{ // Original ELF 0x1425184; independent candidate only.
			var spawnerConfig = spawner.Config;
			Vector3 position = spawner.Position;
			var instance = unitPool.Spawn(spawnerConfig.Prefab, position, Quaternion.identity, null);
			var entity = instance.GetComponent<EntityProvider>().Entity;
			ClearStaleState(instance);
			instance.OnDied -= HandleDeath;
			instance.OnDevoured -= HandleDevour;
			instance.transform.localScale = spawnerConfig.Prefab.transform.localScale;
			instance.SetDeathState(false);
			instance.Initialize(Lifetime.Eternal, spawnerConfig, position);
			float multiplier = abTestService.GetEnemyHpDamageMultiplier(spawnerConfig.LocationNumber);
			if (multiplier != 1f)
			{
				for (int i = 0; i < instance.Stats.Count; i++)
				{
					var stat = instance.Stats[i];
					if (stat.Type == StatType.Health || stat.Type == StatType.Damage)
					{
						double product = stat.BaseValue * (double)multiplier;
						double integral = System.Math.Truncate(product);
						double fraction = product - integral;
						double rounded;
						if (product >= 0d)
						{
							if (fraction == .5d)
								rounded = (((long)integral & 1L) == 0L) ? integral : integral + 1d;
							else
								rounded = System.Math.Floor(product + .5d);
						}
						else
						{
							if (fraction == -.5d)
								rounded = (((long)integral & 1L) == 0L) ? integral : integral - 1d;
							else
								rounded = System.Math.Ceiling(product - .5d);
						}

						instance.Stats[i] = new Stat(Lifetime.Eternal, rounded, stat.Type);
					}
				}

				instance.BuildStatLookup();
				instance.CurrentHealth = instance.GetStatValue(StatType.Health);
			}

			ApplyChainMobSettings(instance, chain);
			if (!spawnerConfig.IsStatic && !instance.IsFlying)
			{
				instance.Agent.enabled = true;
				World.Default.GetStash<NavMeshAgentActiveMarker>().Set(entity);
				instance.LastDestination = instance.CurrentSpawnerPatrolPoint;
				instance.Agent.SetDestination(instance.CurrentSpawnerPatrolPoint);
			}

			enemiesModel.AddEnemy(instance);
			instance.OnDied += HandleDeath;
			instance.OnDevoured += HandleDevour;
			ignoreFarStash.Set(entity);
			dynamicPoiStash.Set(entity);
			mobTagStash.Set(entity, new QuestMobTag { StepKey = stepKey, FocusCamera = focusCamera, SpawnAnimPlayed = false });
			if (focusCamera)
			{
				instance.VisualRoot.localScale = Vector3.zero;
				World.Default.GetStash<UnitSpawnActiveMarker>().Set(entity);
			}

			return instance;
		}

		private void ClearStaleState(EnemyUnit instance)
		{ // Original ELF 0x1425a88; independent candidate only.
			var entity = instance.GetComponent<EntityProvider>().Entity;
			if (deadMarkerStash.Has(entity))
				deadMarkerStash.Remove(entity);
			if (mobTagStash.Has(entity))
				mobTagStash.Remove(entity);
			if (ignoreFarStash.Has(entity))
				ignoreFarStash.Remove(entity);
			if (dynamicPoiStash.Has(entity))
				dynamicPoiStash.Remove(entity);
		}

		private EnemySpawnerConfig BuildConfig(QuestStepDef stepDef, float fightTime, QuestChainDef chain, EnemySpawner referenceSpawner)
		{ // Original ELF 0x1424d18; independent candidate only.
			var location = locationsConfig.Locations.First(item => item.Number == stepDef.LocationNumber);
			return new EnemySpawnerConfig
			{
				Key = "quest_" + stepDef.StepKey,
				Prefab = stepDef.Prefab,
				SpawnArea = referenceSpawner.Config.SpawnArea * 4f + Vector2.one * 10f,
				SpawnAreaRotation = referenceSpawner.Config.SpawnAreaRotation,
				Count = 1,
				Stats = BuildFixedStats(chain),
				WeaponType = location.PreferredWeaponType,
				ArmorConfig = BuildArmorConfig(location),
				IsStatic = false,
				AggroRange = 10f,
				AggroDuration = 8f,
				IsEvent = false,
				LocationNumber = stepDef.LocationNumber,
				StatRewards = new List<StatModifier>(),
				IsDays = stepDef.IsDays,
				OptimumTime = stepDef.IsDays ? stepDef.OptimumTime : SpawnerOptimumTimeDaysConverter.ConvertOptimumToDays(stepDef.OptimumTime),
				AverageFightTime = fightTime,
				Difficulty = 1f
			};
		}

		private float GetFightTime(string stepKey, QuestChainDef chain)
		{ // Original ELF 0x1424c74; independent candidate only.
			return chain.Steps[chain.Steps.Count - 1].StepKey == stepKey ? 25f : 15f;
		}

		private List<Stat> BuildFixedStats(QuestChainDef chain)
		{ // Original ELF 0x1425e08; independent candidate only.
			return new List<Stat>
			{
				new Stat(Lifetime.Eternal, 0, StatType.Health),
				new Stat(Lifetime.Eternal, 0, StatType.Damage),
				new Stat(Lifetime.Eternal, 3.5, StatType.MovementSpeed),
				new Stat(Lifetime.Eternal, (double)3.3f, StatType.AttackRange),
				new Stat(Lifetime.Eternal, (double)chain.MobAttackCooldown, StatType.AttackCooldown)
			};
		}

		private ArmorConfig BuildArmorConfig(LocationConfig location)
		{ // Original ELF 0x1426018; independent candidate only.
			var armor = ScriptableObject.CreateInstance<ArmorConfig>();
			armor.Armors = BuildDefaultArmors(location.PreferredArmorType);
			return armor;
		}

		private List<Armor> BuildDefaultArmors(WeaponType weakness)
		{ // Original ELF 0x142608c; independent candidate only.
			return new List<Armor>
			{
				new Armor
				{
					Type = (WeaponType)0,
					Resist = weakness == (WeaponType)0 ? -0.9f : 0.9f
				},
				new Armor
				{
					Type = (WeaponType)1,
					Resist = weakness == (WeaponType)1 ? -0.9f : 0.9f
				},
				new Armor
				{
					Type = (WeaponType)2,
					Resist = weakness == (WeaponType)2 ? -0.9f : 0.9f
				},
				new Armor
				{
					Type = (WeaponType)3,
					Resist = weakness == (WeaponType)3 ? -0.9f : 0.9f
				}
			};
		}

		private void ApplyChainMobSettings(EnemyUnit unit, QuestChainDef chain)
		{ // Original ELF 0x1425c54; independent candidate only.
			double health = unit.GetStatValue(StatType.Health), damage = unit.GetStatValue(StatType.Damage);
			var dodge = new List<StatModifier>
			{
				new StatModifier
				{
					StatType = StatType.Evasion,
					ModifierType = ModifierType.PostFormulaFlat,
					Source = (ModifierSource)0,
					Value = chain.MobEvasionPercent * 0.01f
				}
			};
			SetStat(unit, StatType.Evasion, 0, dodge);
			SetStat(unit, StatType.HealthRegen, health * (double)chain.MobHealthRegenMaxHealthPercent * (double)0.01f);
			SetStat(unit, StatType.LifestealPercent, (double)(chain.MobLifestealMaxHealthPercent * 0.01f));
			unit.ReflectionDamage = damage * (double)chain.MobReflectionMaxHealthPercent * (double)0.01f;
			unit.BuildStatLookup();
		}

		private Stat GetStat(EnemyUnit unit, StatType statType)
		{ // Original ELF 0x14263e8; independent candidate only.
			return unit.Stats.First(stat => stat.Type == statType);
		}

		private void SetStat(EnemyUnit unit, StatType statType, double baseValue, List<StatModifier> modifiers = null)
		{ // Original ELF 0x142623c; independent candidate only.
			int index = unit.Stats.FindIndex(stat => stat.Type == statType);
			var replacement = new Stat(Lifetime.Eternal, baseValue, statType, modifiers);
			if (index < 0)
				unit.Stats.Add(replacement);
			else
				unit.Stats[index] = replacement;
		}

		private bool TryPickSpawnPosition(Vector3 spawnerCenter, EnemySpawnerConfig config, out Vector3 position)
		{ // Original ELF 0x1425018; independent candidate only.
			int unlocked = playerModel.LastUnlockedLocation.Number;
			Vector3 origin = Vector3.zero;
			if (config.LocationNumber <= unlocked)
				origin = playerModel.Unit is null ? playerModel.LastPosition : playerModel.Unit.transform.position;
			for (int attempt = 0; attempt < 100; attempt++)
			{
				var offset = GetRandomOffset(config.SpawnArea, config.SpawnAreaRotation);
				if (NavMesh.SamplePosition(spawnerCenter + offset, out var hit, 4f, NavMesh.AllAreas) && (config.LocationNumber > unlocked || IsPathReachable(origin, hit.position)))
				{
					position = hit.position;
					return true;
				}
			}

			position = default;
			return false;
		}

		private bool IsPathReachable(Vector3 from, Vector3 to)
		{ // Original ELF 0x1426558; independent candidate only.
			var path = new NavMeshPath();
			return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
		}

		private Vector3 GetRandomOffset(Vector2 spawnArea, float rotation)
		{ // Original ELF 0x14264dc; independent candidate only.
			float x = spawnArea.x * UnityEngine.Random.Range(-0.5f, 0.5f);
			float z = spawnArea.y * UnityEngine.Random.Range(-0.5f, 0.5f);
			return Quaternion.Euler(0, rotation, 0) * new Vector3(x, 0, z);
		}

		private EnemySpawner PickSpawner(int locationNumber)
		{ // Original ELF 0x1424994; independent candidate only.
			var candidates = enemiesModel.Spawners.Where(spawner => spawner.Config.LocationNumber == locationNumber && !spawner.Config.IsEvent && !spawner.Config.IsStatic && !spawner.Config.IsVip && spawner.enabled).ToList();
			if (candidates.Count == 0)
				return null;
			if (candidates.Count == 1)
			{
				lastSpawnerByLocation[locationNumber] = candidates[0];
				return candidates[0];
			}

			lastSpawnerByLocation.TryGetValue(locationNumber, out var last);
			if (last != null)
			{
				var alternatives = candidates.Where(spawner => spawner != last).ToList();
				if (alternatives.Count > 0)
					candidates = alternatives;
			}

			var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
			lastSpawnerByLocation[locationNumber] = selected;
			return selected;
		}

		public bool HasLiveMob(string stepKey)
		{ // Original ELF 0x1426630; independent candidate only.
			return activeByStep.TryGetValue(stepKey, out var active) && active.Unit != null && !active.Unit.IsDead;
		}

		public bool TryGetMobTransform(string stepKey, out Transform transform)
		{ // Original ELF 0x1410dbc; independent candidate only.
			if (activeByStep.TryGetValue(stepKey, out var active) && active.Unit != null && !active.Unit.IsDead)
			{
				transform = active.Unit.transform;
				return true;
			}

			transform = null;
			return false;
		}

		public bool TryGetAnyMobTransform(out Transform transform)
		{ // Original ELF 0x14100e8; independent candidate only.
			foreach (var active in activeByStep.Values)
				if (active.Unit != null && !active.Unit.IsDead)
				{
					transform = active.Unit.transform;
					return true;
				}

			transform = null;
			return false;
		}

		public ParticleSystem GetMobSpawnVfx(string stepKey)
		{ // Original ELF 0x142670c; independent candidate only.
			return activeByStep.TryGetValue(stepKey, out var active) ? active.ReferenceSpawner.SpawnVfx : null;
		}

		private ActiveQuestMob FindActiveMob(EnemyUnit unit)
		{ // Original ELF 0x1426798; independent candidate only.
			foreach (var active in activeByStep.Values)
				if (active.Unit == unit)
					return active;
			return null;
		}

		private void HandleDeath(UnitCore unit)
		{ // Original ELF 0x14269bc; independent candidate only.
			unit.OnDied -= HandleDeath;
			var enemy = (EnemyUnit)unit;
			var active = FindActiveMob(enemy);
			if (active == null)
				return;
			string stepKey = FindStepKey(active);
			var entity = enemy.EntityProvider.Entity;
			if (stepKey != null)
			{
				App.ExecuteCommand(new CompleteQuestStepCommand { StepKey = stepKey });
				QuestFlowLauncher.FocusQuestGiverAfterKill(config, stepKey);
			}

			if (mobTagStash.Has(entity))
				mobTagStash.Remove(entity);
			abilityExecuteEventSystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnKill, Target = enemy });
			enemiesModel.RemoveEnemy(enemy);
			if (userModel.OptimumTime.Value < active.Spawner.Config.OptimumTime && !enemy.IsStatic)
				userModel.OptimumTime.Value = active.Spawner.Config.OptimumTime;
			EnemySpawner.OnDied?.Invoke(enemy);
			if (!enemy.IsFlying && !enemy.DisableAgent && !enemy.IsStatic && enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
				enemy.Agent.isStopped = true;
		}

		private void HandleDevour(UnitCore unit)
		{ // Original ELF 0x14270bc; independent candidate only.
			var enemy = (EnemyUnit)unit;
			var active = FindActiveMob(enemy);
			if (active == null)
				return;
			abilityExecuteEventSystem.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnDevour, Target = enemy });
			enemy.OnDevoured -= HandleDevour;
			DespawnUnit(active);
		}

		private void DespawnUnit(ActiveQuestMob active)
		{ // Original ELF 0x14243f4; independent candidate only.
			string stepKey = FindStepKey(active);
			if (stepKey == null)
				return;
			var unit = active.Unit;
			if (unit != null)
			{
				unit.OnDied -= HandleDeath;
				unit.OnDevoured -= HandleDevour;
				var entity = unit.EntityProvider.Entity;
				if (mobTagStash.Has(entity))
					mobTagStash.Remove(entity);
				if (dynamicPoiStash.Has(entity))
					dynamicPoiStash.Remove(entity);
				enemiesModel.RemoveEnemy(unit);
				unitPool.Despawn(unit);
			}

			activeByStep.Remove(stepKey);
		}

		private string FindStepKey(ActiveQuestMob active)
		{ // Original ELF 0x1426de0; independent candidate only.
			foreach (var pair in activeByStep)
				if (pair.Value == active)
					return pair.Key;
			return null;
		}

		public QuestMobSpawnerService()
		{ // Original ELF 0x142728c; independent candidate only.
			activeByStep = new Dictionary<string, ActiveQuestMob>();
			lastSpawnerByLocation = new Dictionary<int, EnemySpawner>();
			unitPool = new ObjectPool<EnemyUnit>();
			mobTagStash = World.Default.GetStash<QuestMobTag>();
			deadMarkerStash = World.Default.GetStash<UnitDeadMarker>();
			ignoreFarStash = World.Default.GetStash<IgnoreFarMarker>();
			dynamicPoiStash = World.Default.GetStash<DynamicPoiTag>();
		}
	}
}
