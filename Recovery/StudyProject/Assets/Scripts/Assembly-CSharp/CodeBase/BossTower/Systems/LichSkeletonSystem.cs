using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Codebase.Vfx;
using Scellecs.Morpeh;
using UnityEngine;
using System;
using Codebase.Enemy;
using Codebase.Unit;
using DG.Tweening;
using Scellecs.Morpeh.Providers;
using UnityEngine.AI;

namespace Codebase.BossTower.Systems
{
	public class LichSkeletonSystem : SystemBase
	{
		private readonly List<Entity> upgradeQueue = new List<Entity>();

		private Filter filter;

		private Filter skeletonsFilter;

		private Filter deadSkeletonsFilter;

		[Inject]
		private Stash<LichBossComponent> lichStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<LichSkeletonTag> skeletonTagStash;

		[Inject]
		private Stash<UnitHasDeathAnimMarker> unitHasDeathAnimStash;

		[Inject]
		private BossMinionService minionService;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private BossArenaSystem bossArenaSystem;

		[Inject]
		private WorldVfxService worldVfxService;

		private Filter lichDeathFilter;

		private Filter fightFilter;

		public override void OnAwake()
		{
			// Original ELF VA 0x156ce14.
			filter = Filter.With<LichBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			fightFilter = Filter.With<BossFightState>().Build();
			lichDeathFilter = Filter.With<LichBossComponent>().With<UnitComponent>().With<UnitDeadMarker>().Build();
			skeletonsFilter = Filter.With<LichSkeletonTag>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			deadSkeletonsFilter = Filter.With<LichSkeletonTag>().With<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ELF VA 0x156d1f0.
			if (!bossTowerModel.IsFighting) return;
			UpdateSkeletonSpawn(deltaTime);
			UpdateSkeletonUpgrade(deltaTime);
			if (!fightFilter.IsEmpty() && !skeletonsFilter.IsEmpty() && filter.IsEmpty())
				foreach (Entity entity in skeletonsFilter)
				{
					UnitCore unit = unitStash.Get(entity).Core;
					Entity owner = skeletonTagStash.Get(entity).OwnerLich;
					if (!World.IsDisposed(owner))
					{
						ref LichBossComponent lich = ref lichStash.Get(owner);
						if (lich.SpawnVfxPrefab != null)
							worldVfxService.Spawn(lich.SpawnVfxPrefab, unit.transform.position, Quaternion.identity, lich.SpawnVfxLifetime);
					}
					minionService.Despawn((EnemyUnit)unit);
				}
		}

		private void UpdateSkeletonSpawn(float deltaTime)
		{
			// Original ELF VA 0x156d574: cap is checked before the whole spawn batch.
			int count = 0;
			foreach (Entity entity in skeletonsFilter) count++;
			if (count > 29) return;
			foreach (Entity entity in filter)
			{
				ref LichBossComponent lich = ref lichStash.Get(entity);
				lich.SkeletonTimer -= deltaTime;
				if (lich.SkeletonTimer <= 0f)
				{
					for (int i = 0; i < lich.SkeletonSpawnCount; i++) SpawnSkeleton(entity, ref lich, i);
					lich.SkeletonTimer = lich.SkeletonInterval;
				}
			}
		}

		public bool TryGetSkeletonCentroid(Entity lichEntity, out Vector3 centroid)
		{
			// Original ELF VA 0x156e474: a failed NavMesh sample has no usable centroid.
			if (lichStash.Has(lichEntity))
			{
				Vector3 sum = Vector3.zero;
				int count = 0;
				foreach (Entity entity in skeletonsFilter)
					if (skeletonTagStash.Get(entity).OwnerLich == lichEntity)
					{
						sum += unitStash.Get(entity).Core.transform.position;
						count++;
					}
				if (count != 0 && NavMesh.SamplePosition(sum / count, out NavMeshHit hit, 5f, NavMesh.AllAreas))
				{
					centroid = hit.position;
					return true;
				}
			}
			centroid = Vector3.zero;
			return false;
		}

		public void OnRaidEnded()
		{
			// Original ELF VA 0x156e6d4; dead bosses precede live bosses.
			foreach (Entity entity in lichDeathFilter) lichStash.Remove(entity);
			foreach (Entity entity in filter) lichStash.Remove(entity);
			foreach (Entity entity in skeletonsFilter) skeletonTagStash.Remove(entity);
			foreach (Entity entity in deadSkeletonsFilter) skeletonTagStash.Remove(entity);
		}

		private void UpdateSkeletonUpgrade(float deltaTime)
		{
			// Original ELF VA 0x156d730; queue the old entities before replacing pooled units.
			upgradeQueue.Clear();
			foreach (Entity entity in skeletonsFilter)
			{
				ref LichSkeletonTag tag = ref skeletonTagStash.Get(entity);
				if (World.IsDisposed(tag.OwnerLich)) continue;
				ref LichBossComponent lich = ref lichStash.Get(tag.OwnerLich);
				if (lich.EvolutionVariants != null && tag.EvolutionLevel + 1 < lich.MaxEvolutionLevel)
				{
					EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
					if (!(unit.CurrentHealth <= 0.0))
					{
						tag.LifeTimer += deltaTime;
						if (!(tag.LifeTimer < lich.EvolutionVariants[tag.EvolutionLevel].UpgradeTime)) upgradeQueue.Add(entity);
					}
				}
			}
			foreach (Entity entity in upgradeQueue)
			{
				ref LichSkeletonTag tag = ref skeletonTagStash.Get(entity);
				if (World.IsDisposed(tag.OwnerLich)) continue;
				ref LichBossComponent lich = ref lichStash.Get(tag.OwnerLich);
				int nextLevel = tag.EvolutionLevel + 1;
				SkeletonVariant variant = lich.EvolutionVariants[nextLevel];
				if (variant.Prefab == null) continue;
				EnemyUnit oldUnit = (EnemyUnit)unitStash.Get(entity).Core;
				double maximum = oldUnit.GetStatValue(StatType.Health, true);
				if (!(maximum > 0.0)) continue;
				double ratio = oldUnit.CurrentHealth / maximum;
				if (ratio <= 0.0) continue;
				Vector3 position = oldUnit.transform.position;
				Quaternion rotation = oldUnit.transform.rotation;
				ParticleSystem vfx = variant.VfxPrefab != null ? variant.VfxPrefab : lich.SpawnVfxPrefab;
				if (vfx != null) worldVfxService.Spawn(vfx, position, rotation, lich.SpawnVfxLifetime);
				EnemyUnit unit = minionService.Spawn(Guid.NewGuid().ToString(), variant.Prefab, position, rotation, lich.SkeletonConfig, false, variant.BakedStats, false);
				unit.VisualRoot.DOScale(Vector3.one, .5f).From(Vector3.zero, true, false).SetEase(Ease.InOutQuad);
				Entity newEntity = unit.GetComponent<EntityProvider>().Entity;
				unitHasDeathAnimStash.Set(newEntity, default);
				skeletonTagStash.Set(newEntity, new LichSkeletonTag { OwnerLich = tag.OwnerLich, LifeTimer = 0f, EvolutionLevel = nextLevel });
				unit.CurrentHealth = ratio * unit.GetStatValue(StatType.Health, true);
				minionService.Despawn(oldUnit);
			}
		}

		private void SpawnSkeleton(Entity lichEntity, ref LichBossComponent lich, int index)
		{
			// Original ELF VA 0x156e128; index is unused by the shipped method.
			if (lich.SkeletonConfig == null || lich.EvolutionVariants == null || lich.EvolutionVariants.Length == 0) return;
			if (!bossArenaSystem.TryGetActive(out BossArenaComponent arena)) return;
			SkeletonVariant variant = lich.EvolutionVariants[0];
			if (variant.Prefab == null) return;
			Vector3 position = arena.BossSpawnPoint.position;
			Vector3 bossPosition = unitStash.Get(lichEntity).Core.transform.position;
			Vector3 random = UnityEngine.Random.insideUnitSphere;
			Vector3 candidate = bossPosition + new Vector3(random.x * lich.SkeletonSpawnRadius, 0f, random.z * lich.SkeletonSpawnRadius);
			if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas)) position = hit.position;
			ParticleSystem vfx = variant.VfxPrefab != null ? variant.VfxPrefab : lich.SpawnVfxPrefab;
			if (vfx != null) worldVfxService.Spawn(vfx, position, Quaternion.identity, lich.SpawnVfxLifetime);
			EnemyUnit unit = minionService.Spawn(Guid.NewGuid().ToString(), variant.Prefab, position, lich.SkeletonConfig, false, variant.BakedStats, false);
			Entity entity = unit.GetComponent<EntityProvider>().Entity;
			unitHasDeathAnimStash.Set(entity, default);
			skeletonTagStash.Set(entity, new LichSkeletonTag { OwnerLich = lichEntity, LifeTimer = 0f, EvolutionLevel = 0 });
		}
	}
}
