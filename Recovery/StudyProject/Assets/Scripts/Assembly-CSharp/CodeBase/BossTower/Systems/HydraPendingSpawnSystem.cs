using Codebase.Enemy;
using DG.Tweening;
using UnityEngine.AI;
using UnityEngine;
using Codebase.Unit.Components;
using CodeBase.Audio;
using System;
using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Vfx;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Systems
{
	public class HydraPendingSpawnSystem : SystemBase
	{
		private const float ProjectileSpawnDelay = 0.5f;

		private Filter waitingFilter;

		private Filter movingFilter;

		[Inject]
		private Stash<BossPendingSpawnMarker> pendingMarkerStash;

		[Inject]
		private Stash<HydraPendingSpawnComponent> pendingSpawnStash;

		[Inject]
		private Stash<HydraProjectileComponent> projectileStash;

		[Inject]
		private BossSpawnService bossSpawnService;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private WorldVfxService worldVfxService;

		private readonly List<Entity> toActivate;

		private readonly List<Entity> toFinish;

        // Native ELF VA 0x156b478.
        public HydraPendingSpawnSystem() { toActivate = new List<Entity>(); toFinish = new List<Entity>(); }

		public override void OnAwake()
		{
			// Native ELF VA 0x1569ce8.
			waitingFilter = Filter.With<BossPendingSpawnMarker>().With<HydraPendingSpawnComponent>().Without<HydraProjectileComponent>().Build();
			movingFilter = Filter.With<BossPendingSpawnMarker>().With<HydraPendingSpawnComponent>().With<HydraProjectileComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x1569ed4. Stage list mutations separately from filter enumeration.
			toActivate.Clear(); toFinish.Clear();
			foreach (Entity entity in waitingFilter)
			{
			    ref HydraPendingSpawnComponent pending = ref pendingSpawnStash.Get(entity);
			    pending.ProjectileDelay -= deltaTime;
			    if (pending.ProjectileDelay <= 0f) toActivate.Add(entity);
			}
			foreach (Entity entity in toActivate)
			{
			    ref HydraPendingSpawnComponent pending = ref pendingSpawnStash.Get(entity);
			    GameObject projectile = UnityEngine.Object.Instantiate(pending.HydraData.SpawnProjectilePrefab, pending.StartPosition, Quaternion.identity);
			    projectileStash.Set(entity, new HydraProjectileComponent { SpawnProjectile = projectile });
			}
			foreach (Entity entity in movingFilter)
			{
			    ref HydraPendingSpawnComponent pending = ref pendingSpawnStash.Get(entity);
			    GameObject projectile = projectileStash.Get(entity).SpawnProjectile;
			    Vector3 start = pending.StartPosition; start.y = 0f;
			    Vector3 target = pending.TargetPosition; target.y = 0f;
			    Vector3 current = projectile.transform.position; current.y = 0f;
			    float distance = Vector3.Distance(start, target);
			    if (distance < .001f) { toFinish.Add(entity); continue; }
			    float progress = Mathf.Min(Vector3.Distance(start, current) / distance, 1f);
			    float phase = progress + progress - 1f;
			    float speed = (phase * (phase * .4f) + .6f) * pending.HydraData.SpawnProjectileSpeed;
			    Vector3 next = Vector3.MoveTowards(current, target, speed * deltaTime);
			    float nextProgress = Mathf.Min(Vector3.Distance(start, next) / distance, 1f);
			    projectile.transform.position = new Vector3(next.x, ArcY(nextProgress) + (pending.StartPosition.y + nextProgress * (pending.TargetPosition.y - pending.StartPosition.y)), next.z);
			    if (Vector3.Distance(next, target) < .05f) toFinish.Add(entity);
			}
			foreach (Entity entity in toFinish)
			{
			    ref HydraPendingSpawnComponent pending = ref pendingSpawnStash.Get(entity);
			    GameObject projectile = projectileStash.Get(entity).SpawnProjectile;
			    Vector3 spawnPosition = pending.TargetPosition;
			    if (NavMesh.SamplePosition(pending.TargetPosition, out NavMeshHit navHit, 5f, NavMesh.AllAreas)) spawnPosition = navHit.position;
			    Vector3 vfxPosition = spawnPosition;
			    if (Physics.Raycast(pending.TargetPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f)) vfxPosition = hit.point + Vector3.up * .01f;
			    EnemyUnit child = bossSpawnService.SpawnHydraChild(pending.BossFightKey, pending.ChildPrefab, spawnPosition,
			        pending.ChildConfig, pending.HydraData, pending.ChildPrefab.transform.localScale * pending.HydraData.CurrentScaleMultiplier,
			        pending.Index, pending.ChildStats, pending.MarkStacks);
			    child.transform.DOScale(child.OriginalScale, .9f).From(Vector3.one * .1f);
			    worldVfxService.Spawn(pending.HydraData.DeathVfxPrefab, vfxPosition, Quaternion.identity, 3f);
			    GameContext.Get<IAudioPlayer>().PlayAtPosition("slime_fall", pending.TargetPosition);
			    if (pending.HydraData.OriginalMaterial != null && pending.ChildPrefab == pending.HydraData.Prefab) child.SetMaterial(pending.HydraData.OriginalMaterial);
			    UnityEngine.Object.Destroy(projectile); projectileStash.Remove(entity); pendingMarkerStash.Remove(entity); pendingSpawnStash.Remove(entity);
			}
		}

		private static float ArcY(float t)
		{
			// Native ELF VA 0x156afcc; retain original float operation order.
			return t * 32f * (1f - t);
		}

		public void OnRaidEnded()
		{
			// Native ELF VA 0x156afe4; original global pending marker cleanup.
			var entities = new List<Entity>();
			foreach (Entity entity in World.Filter.With<BossPendingSpawnMarker>().Build()) entities.Add(entity);
			foreach (Entity entity in entities)
			{
			    if (projectileStash.Has(entity)) { UnityEngine.Object.Destroy(projectileStash.Get(entity).SpawnProjectile); projectileStash.Remove(entity); }
			    pendingMarkerStash.Remove(entity); pendingSpawnStash.Remove(entity);
			}
		}
	}
}
