using UniMob;
using Scellecs.Morpeh.Providers;
using Codebase.Abilities.Events;
using Codebase.Abilities;
using System.Collections.Generic;
using Codebase.Abilities.Systems;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Utilities.ObjectPool;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower
{
	public class BossMinionService
	{
		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private AbilityExecuteEventSystem abilitySystem;

		private readonly ObjectPool<EnemyUnit> pool;

		private readonly HashSet<EnemyUnit> registeredMinions;

		private readonly Stash<BossArenaUnitTag> arenaUnitStash;

		private readonly Stash<BossMinionUnitMarker> minionMarkerStash;

		private readonly Stash<BossArenaUnitHasHealthBarMarker> arenaUnitWithHealthBar;

		private readonly Stash<UnitDeadMarker> deadMarkerStash;

		private readonly Stash<MarkComponent> markerStash;

		private readonly Stash<UnitHasDeathAnimMarker> unitHasDeathAnimStash;

		[Inject]
		private Stash<UnitWithoutRegenMarker> unitWithoutRegenStash;

		[Inject]
		private Stash<IgnoreFarMarker> ignoreFarMarkerStash;

        // Native ELF VA 0x15515b0; the missing constructor binds the original default world.
        public BossMinionService()
        {
            pool = new ObjectPool<EnemyUnit>();
            registeredMinions = new HashSet<EnemyUnit>();
            World world = World.Default;
            arenaUnitStash = world.GetStash<BossArenaUnitTag>();
            minionMarkerStash = world.GetStash<BossMinionUnitMarker>();
            arenaUnitWithHealthBar = world.GetStash<BossArenaUnitHasHealthBarMarker>();
            deadMarkerStash = world.GetStash<UnitDeadMarker>();
            markerStash = world.GetStash<MarkComponent>();
            unitHasDeathAnimStash = world.GetStash<UnitHasDeathAnimMarker>();
        }

		public EnemyUnit Spawn(string unitGuid, EnemyUnit prefab, Vector3 position, EnemySpawnerConfig config, bool countInHpBar = false, List<Stat> stats = null, bool hasDeathAnim = false)
		{
			// Native ELF VA 0x155065c.
			return Spawn(unitGuid, prefab, position, Quaternion.identity, config, countInHpBar, stats, hasDeathAnim);
		}

		public EnemyUnit Spawn(string unitGuid, EnemyUnit prefab, Vector3 position, Quaternion rotation, EnemySpawnerConfig config, bool countInHpBar = false, List<Stat> stats = null, bool hasDeathAnim = false)
		{
			// Native ELF VA 0x1550720; preserve cleanup before original stats initialization and registration.
			EnemyUnit instance = pool.Spawn(prefab, position, rotation, null);
			CleanupStaleState(instance);
			instance.Initialize(Lifetime.Eternal, config, position, stats);
			Register(unitGuid, instance, countInHpBar, hasDeathAnim);
			return instance;
		}

		private void CleanupStaleState(EnemyUnit instance)
		{
			// Native ELF VA 0x1550860. Other owners' listeners and state are retained.
			instance.OnDied -= OnDeath; instance.OnDevoured -= OnDevour;
			registeredMinions.Remove(instance);
			Entity entity = instance.GetComponent<EntityProvider>().Entity;
			if (deadMarkerStash.Has(entity)) deadMarkerStash.Remove(entity);
			if (markerStash.Has(entity)) markerStash.Remove(entity);
		}

		public void Register(string unitGuid, EnemyUnit minion, bool countInHpBar = false, bool hasDeathAnim = false)
		{
			// Native ELF VA 0x1550af0; the duplicate guard precedes every side effect.
			if (registeredMinions.Contains(minion)) return;
			registeredMinions.Add(minion);
			enemiesModel.AddEnemy(minion);
			Entity entity = minion.GetComponent<EntityProvider>().Entity;
			arenaUnitStash.Set(entity, new BossArenaUnitTag { UnitGuid = unitGuid });
			minionMarkerStash.Set(entity);
			if (countInHpBar) arenaUnitWithHealthBar.Set(entity);
			if (hasDeathAnim) unitHasDeathAnimStash.Set(entity);
			unitWithoutRegenStash.Set(entity); ignoreFarMarkerStash.Set(entity);
			minion.OnDied += OnDeath; minion.OnDevoured += OnDevour;
		}

		public void Despawn(EnemyUnit minion)
		{
			// Native ELF VA 0x1550e40. Death notification is not synthesized by explicit despawn.
			if (!registeredMinions.Contains(minion)) return;
			Entity entity = minion.GetComponent<EntityProvider>().Entity;
			minion.OnDied -= OnDeath; minion.OnDevoured -= OnDevour;
			minion.CurrentHealth = 0;
			enemiesModel.RemoveEnemy(minion); registeredMinions.Remove(minion);
			arenaUnitStash.Remove(entity); deadMarkerStash.Set(entity);
			pool.Despawn(minion);
		}

		private void OnDeath(UnitCore unit)
		{
			// Native ELF VA 0x15510ec; OnKill is deferred, while the original corpse remains for its devour path.
			unit.OnDied -= OnDeath;
			EnemyUnit minion = (EnemyUnit)unit;
			abilitySystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnKill, Target = minion });
			enemiesModel.RemoveEnemy(minion); registeredMinions.Remove(minion);
			if (!unitHasDeathAnimStash.Has(minion.EntityProvider.Entity)) EnemySpawner.OnDied?.Invoke(minion);
		}

		private void OnDevour(UnitCore unit)
		{
			// Native ELF VA 0x1551350; the immediate ability trigger occurs before detachment and pooling.
			EnemyUnit minion = (EnemyUnit)unit;
			abilitySystem.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnDevour, Target = minion });
			minion.OnDevoured -= OnDevour;
			arenaUnitStash.Remove(minion.GetComponent<EntityProvider>().Entity);
			registeredMinions.Remove(minion);
			pool.Despawn(minion);
		}
	}
}
