using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Scellecs.Morpeh;
using Codebase.BossTower.Helpers;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class MageBossSystem : SystemBase
	{
		private Filter bossFilter;

		private Filter retreatingFilter;

		private Event<WeaponHitEvent> hitEvent;

		[Inject]
		private Stash<MageBossComponent> mageStash;

		[Inject]
		private Stash<BossRetreatState> retreatStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private BossArenaSystem bossArenaSystem;

		public override void OnAwake()
		{
			// Original ELF VA 0x156ea98.
			bossFilter = Filter.With<MageBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			retreatingFilter = Filter.With<MageBossComponent>().With<BossRetreatState>().Without<UnitDeadMarker>().Build();
			hitEvent = World.GetEvent<WeaponHitEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!playerModel.InBossTowerArena) return;
			ProcessPlayerHitsOnBoss();
			ProcessBossAttacks();
			UpdateBossRetreat(deltaTime);
		}

		private void ProcessPlayerHitsOnBoss()
		{
			// Original ELF VA 0x156ed0c: the shared iterator selects by target component.
			foreach (var pair in hitEvent.IterateHitsOn(mageStash))
			{
				Entity entity = pair.Item2;
				ref MageBossComponent mage = ref mageStash.Get(entity);
				mage.CurrentHitCount = unchecked(mage.CurrentHitCount + 1);
				if (mage.CurrentHitCount >= mage.HitsToTeleport)
				{
					TeleportToCorner(entity);
					mage.CurrentHitCount = 0;
				}
			}
		}

		private void ProcessBossAttacks()
		{
			// Original ELF VA 0x156f030: exactly one retreat per positive attack cooldown cycle.
			foreach (Entity entity in bossFilter)
			{
				ref MageBossComponent mage = ref mageStash.Get(entity);
				EnemyUnit enemy = (EnemyUnit)unitStash.Get(entity).Core;
				if (enemy.AttackCooldown > 0f && !mage.HasAttackedThisCycle)
				{
					mage.HasAttackedThisCycle = true;
					StartRetreat(entity, ref mage, enemy);
				}
				else if (enemy.AttackCooldown <= 0f) mage.HasAttackedThisCycle = false;
			}
		}

		private void StartRetreat(Entity entity, ref MageBossComponent mage, EnemyUnit enemy)
		{
			// Original ELF VA 0x156f508; vertical difference participates before flattening.
			if (retreatStash.Has(entity)) return;
			Vector3 position = enemy.transform.position;
			Vector3 direction = (position - playerModel.Unit.transform.position).normalized;
			direction.y = 0f;
			Vector3 target = position + direction * mage.RetreatDistance;
			retreatStash.Set(entity, new BossRetreatState { Timer = mage.RetreatDuration, Target = target });
			enemy.RetreatTimer = mage.RetreatDuration;
			enemy.LastDestination = target;
			if (!enemy.IsFlying) enemy.Agent.SetDestination(target);
		}

		private void TeleportToCorner(Entity entity)
		{
			// Original ELF VA 0x156f36c; no new NavMesh projection or retreat-timer reset.
			if (!bossArenaSystem.TryGetActive(out BossArenaComponent arena)) return;
			EnemyUnit enemy = (EnemyUnit)unitStash.Get(entity).Core;
			Vector3 corner = arena.GetRandomCorner();
			if (enemy.IsFlying) enemy.transform.position = corner;
			else { enemy.Agent.Warp(corner); enemy.Agent.ResetPath(); }
			enemy.LastDestination = null;
			retreatStash.Remove(entity);
		}

		private void UpdateBossRetreat(float deltaTime)
		{
			// Original ELF VA 0x156f210; NaN timers survive the original B.HI branch.
			foreach (Entity entity in retreatingFilter)
			{
				ref BossRetreatState retreat = ref retreatStash.Get(entity);
				retreat.Timer -= deltaTime;
				if (retreat.Timer <= 0f) retreatStash.Remove(entity);
			}
		}
	}
}
