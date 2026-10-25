using UniMob;
using Codebase.Unit;
using System.Linq;
using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Vfx;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class HydraDeathSystem : SystemBase
	{
		private Filter allHydraFilter;

		private Filter pendingSpawnFilter;

		[Inject]
		private Stash<HydraBossComponent> hydraStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<MarkComponent> markStash;

		[Inject]
		private Stash<BossTowerBossTag> bossTowerTagStash;

		[Inject]
		private Stash<BossPendingSpawnMarker> pendingMarkerStash;

		[Inject]
		private Stash<HydraPendingSpawnComponent> pendingSpawnStash;

		[Inject]
		private Stash<HydraProjectileComponent> projectileStash;

		[Inject]
		private BossSpawnService bossSpawnService;

		[Inject]
		private BossArenaSystem bossArenaSystem;

		[Inject]
		private WorldVfxService worldVfxService;

		private Filter fightState;

		private Event<UnitDiedEvent> unitDiedEvent;

		public override void OnAwake()
		{
			// Native ELF VA 0x1568184.
			unitDiedEvent = World.GetEvent<UnitDiedEvent>();
			allHydraFilter = Filter.With<HydraBossComponent>().Build();
			pendingSpawnFilter = Filter.With<HydraPendingSpawnComponent>().Build();
			fightState = Filter.With<BossFightState>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x1568368; deaths are consumed from the published, not scheduled, snapshot.
			if (fightState.IsEmpty()) return;
			foreach (UnitDiedEvent died in unitDiedEvent.BatchedChanges)
			{
			    Entity entity = died.Entity;
			    if (!hydraStash.Has(entity)) continue;
			    ref HydraBossComponent hydra = ref hydraStash.Get(entity);
			    Vector3 position = unitStash.Get(entity).Core.transform.position;
			    bool hasMark = markStash.Has(entity);
			    MarkComponent mark = hasMark ? markStash.Get(entity) : default;
			    if (hydra.RemainingSpawns <= 0) continue;
			    string key = bossTowerTagStash.Get(entity).BossFightKey;
			    Vector3[] targets;
			    if (bossArenaSystem.TryGetActive(out BossArenaComponent arena) && arena.SpiritSpawnPoints != null && arena.SpiritSpawnPoints.Length > 0)
			        targets = SelectTargetPositions(arena.SpiritSpawnPoints, position, hydra.SpawnCount);
			    else
			    {
			        targets = new Vector3[hydra.SpawnCount];
			        for (int i = 0; i < targets.Length; i++) targets[i] = position;
			    }
			    for (int i = 0; i < hydra.SpawnCount; i++) CreatePendingSpawn(ref hydra, key, position, hasMark, mark, i, targets[i]);
			}
		}

		public void OnRaidEnded()
		{
			// Native ELF VA 0x1568f60. Snapshot pending entities before removing original components.
			foreach (Entity entity in allHydraFilter) hydraStash.Remove(entity);
			var pending = new List<Entity>();
			foreach (Entity entity in pendingSpawnFilter) pending.Add(entity);
			foreach (Entity entity in pending)
			{
			    if (projectileStash.Has(entity)) { UnityEngine.Object.Destroy(projectileStash.Get(entity).SpawnProjectile); projectileStash.Remove(entity); }
			    pendingMarkerStash.Remove(entity); pendingSpawnStash.Remove(entity);
			}
		}

		private void CreatePendingSpawn(ref HydraBossComponent hydra, string bossTagBossFightKey, Vector3 position, bool hasMark, MarkComponent parentMark, int index, Vector3 targetPosition)
		{
			// Native ELF VA 0x1568b68; generation selects the original baked stats/prefab before decrementing remaining splits.
			int generation = hydra.MaxSplits - hydra.RemainingSpawns;
			List<Stat> stats = hydra.BakedStatsPerLevel != null && generation < hydra.BakedStatsPerLevel.Length ? hydra.BakedStatsPerLevel[generation] : null;
			EnemySpawnerConfig childConfig = stats == null ? CreateChildConfig(hydra.ParentConfig) : hydra.ParentConfig;
			EnemyUnit prefab = generation < hydra.SplitPrefabs.Length && hydra.SplitPrefabs[generation] != null ? hydra.SplitPrefabs[generation] : hydra.SplitPrefabs[hydra.SplitPrefabs.Length - 1];
			HydraBossComponent child = hydra;
			child.RemainingSpawns = hydra.RemainingSpawns - 1;
			child.CurrentScaleMultiplier = hydra.CurrentScaleMultiplier * hydra.SizeMultiplier;
			child.ParentConfig = childConfig;
			child.OriginalScale = prefab.transform.localScale;
			Entity pending = World.CreateEntity();
			pendingMarkerStash.Set(pending);
			pendingSpawnStash.Set(pending, new HydraPendingSpawnComponent {
			    HydraData = child, BossFightKey = bossTagBossFightKey, StartPosition = position, TargetPosition = targetPosition,
			    ChildPrefab = prefab, ChildConfig = childConfig, ChildStats = stats, Index = index,
			    HasMark = hasMark && parentMark.Stacks > 0, MarkStacks = hasMark ? parentMark.Stacks : 0,
			    ProjectileDelay = generation > 0 ? 1f : .6f
			});
		}

		private EnemySpawnerConfig CreateChildConfig(EnemySpawnerConfig parentConfig)
		{
			// Native ELF VA 0x1569444; clone lambda 0x1569628 preserves only original base/type.
			return BossSpawnService.CreateBossFightConfig(parentConfig.Key, parentConfig.Prefab,
			    parentConfig.Stats.Select(stat => new Stat(Lifetime.Eternal, stat.BaseValue, stat.Type)).ToList(),
			    parentConfig.WeaponType, parentConfig.ArmorConfig, parentConfig.AggroRange, parentConfig.AggroDuration,
			    parentConfig.IsFlying, parentConfig.AverageFightTime, parentConfig.Count, parentConfig.Difficulty);
		}

		private static Vector3[] SelectTargetPositions(Transform[] points, Vector3 origin, int count)
		{
			// Native ELF VA 0x1568740; original selectors 0x15696b4/728/710.
			Vector3[] candidates = points.Where(point => point != null).OrderByDescending(point => (point.position - origin).sqrMagnitude).Take(3).Select(point => point.position).ToArray();
			Vector3[] result = new Vector3[count];
			if (candidates.Length == 0) { for (int i = 0; i < count; i++) result[i] = origin; return result; }
			bool[] used = new bool[candidates.Length]; int unique = 0;
			for (int i = 0; i < count; i++)
			{
			    int index;
			    if (unique < candidates.Length)
			    {
			        do { index = UnityEngine.Random.Range(0, candidates.Length); } while (used[index]);
			        used[index] = true; unique++;
			    }
			    else index = UnityEngine.Random.Range(0, candidates.Length);
			    result[i] = candidates[index];
			}
			return result;
		}
	}
}
