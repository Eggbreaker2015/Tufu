using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Skills;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;
using Codebase.Unit.Events;

namespace Codebase.Enemy
{
	public class EnemyAttackSystem : MonoBehaviour
	{
		private PlayerModel playerModel;
		private EnemiesModel enemiesModel;
		private PetsModel petsModel;
		[Inject]
		private World world;
		private Stash<BuffStunComponent> stunStash;
		private Stash<UnitWaitingAttackMarker> waitingMarkerStash;
		private Stash<EnemyCanEscapeMarker> escapeMarkerStash;
		private Stash<UnitSpawnActiveMarker> spawnActiveStash;
		private Stash<BossArenaUnitTag> bossArenaUnitStash;
		private Stash<VampireSpiritTag> vampireSpiritTagStash;
		private NavMeshPath path;
		private SkillService skillService;
		// Ranged enemies: extra distance tolerated while already holding position (no stop-go jitter at the edge).
		private const float RangedHoldSlack = 1f;
		// Ranged enemies: closer than this the line of fire is not checked (the navmesh may not join the two units).
		private const float LineOfFireIgnoreDistance = 3f;
		private const float LineOfFireInterval = 0.25f;
		[Inject]
		private LichSkeletonSystem lichSkeletonSystem;
		private PlayerUnit PlayerUnit
		{
			get
			{
				return playerModel.Unit;
			}
		}

		private void Start()
		{
			playerModel = GameContext.Get<PlayerModel>();
			enemiesModel = GameContext.Get<EnemiesModel>();
			petsModel = GameContext.Get<PetsModel>();
			stunStash = world.GetStash<BuffStunComponent>();
			waitingMarkerStash = world.GetStash<UnitWaitingAttackMarker>();
			escapeMarkerStash = world.GetStash<EnemyCanEscapeMarker>();
			spawnActiveStash = world.GetStash<UnitSpawnActiveMarker>();
			bossArenaUnitStash = world.GetStash<BossArenaUnitTag>();
			vampireSpiritTagStash = world.GetStash<VampireSpiritTag>();
			path = new NavMeshPath();
		}

		private void Update()
		{
			PlayerModel player = playerModel;
			float deltaTime = Time.deltaTime;
			foreach (EnemyUnit enemy in enemiesModel.NonStaticEnemies)
			{
				if (enemy.IsDead || enemy.IsFar)
					continue;
				Entity entity = enemy.EntityProvider.Entity;
				if (IsInSpawnAnim(entity))
				{
					if (!enemy.IsFlying && enemy.Agent.isOnNavMesh)
					{
						enemy.Agent.isStopped = true;
						enemy.Agent.updateRotation = false;
					}
				}
				else if (enemy.IsFlying)
					HandleFlyingEnemy(enemy, entity, player, deltaTime);
				else
					HandleGroundEnemy(enemy, entity, player, deltaTime);
			}
		}

		private bool IsInSpawnAnim(Entity entity)
		{
			return spawnActiveStash.Has(entity);
		}

		private void HandleGroundEnemy(EnemyUnit enemy, Entity entity, PlayerModel player, float deltaTime)
		{
			if (FriendlyDebuffs.IsDisplaced(enemy) || FriendlyDebuffs.IsChanneling(enemy))
			{
				if (enemy.Agent != null && enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.isStopped = true;
					enemy.Agent.updateRotation = false;
				}
				return;
			}
			if (vampireSpiritTagStash.Has(entity))
				return;
			if (escapeMarkerStash.Has(entity))
			{
				HandleEscapingGroundEnemy(enemy, entity, player, deltaTime);
				return;
			}

			if (stunStash.Has(entity) || enemy.IsCasting)
			{
				if (enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.updateRotation = false;
					enemy.Agent.isStopped = true;
				}

				return;
			}

			enemy.Agent.speed = (float)enemy.GetStatValue(StatType.MovementSpeed) * (enemy.RetreatTimer > 0f ? 2f : 1f);
			enemy.RetreatTimer -= deltaTime;
			if (enemy.AttackCooldown > 0f)
				enemy.AttackCooldown -= deltaTime;
			float attackRange = GetAttackRange(enemy);
			float playerDistanceSquared = SqrDistance(enemy, PlayerUnit);
			// Pet mode: the nearest of the player and the living pets is fought; the 30 m reset stays player-based.
			UnitCore aggroTarget = SelectAggroTarget(enemy, playerDistanceSquared, true, out float distanceSquared);
			if (playerDistanceSquared > 900f && !enemy.IsDead && !bossArenaUnitStash.Has(entity))
				enemy.CurrentHealth = enemy.GetStatValue(StatType.Health);
			bool playerUnavailable = player.Unit.IsSleeping || !player.CanMove || player.Unit.IsDying;
			bool retreat = (distanceSquared > enemy.AggroRange * enemy.AggroRange && enemy.AggroTimer <= 0f && !IsTargetedByAllies(enemy)) || enemy.RetreatTimer > 0f;
			if (retreat || playerUnavailable || waitingMarkerStash.Has(entity))
			{
				if (enemy.InCombat)
				{
					enemy.IsReturningHome = !bossArenaUnitStash.Has(entity);
					enemy.InCombat = false;
				}
				ClearAggroTarget(enemy);
				enemy.RangedHolding = false;
				enemy.RangedKiteTimer = 0f;
				enemy.LineOfFireTimer = 0f;
				Vector3 target = enemy.CurrentSpawnerPatrolPoint;
				if (enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.updateRotation = true;
					enemy.Agent.isStopped = false;
				}

				if (!enemy.LastDestination.HasValue || Vector3.Distance(enemy.LastDestination.Value, target) > 0.5f)
				{
					if (enemy.Agent.isOnNavMesh)
						enemy.Agent.SetDestination(target);
					enemy.LastDestination = target;
				}

				if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.1f && enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.updateRotation = false;
					enemy.Agent.isStopped = true;
					enemy.IsReturningHome = false;
				}

				return;
			}

			enemy.InCombat = true;
			enemy.IsReturningHome = false;
			enemy.AggroTimer -= deltaTime;
			if (distanceSquared < enemy.AggroRange * enemy.AggroRange)
				enemy.AggroTimer = enemy.AggroDuration;
			if (enemy.Ranged != null)
			{
				HandleRangedGroundCombat(enemy, aggroTarget, distanceSquared, attackRange, deltaTime);
				return;
			}

			if (distanceSquared < attackRange * attackRange)
			{
				Attack(enemy);
				if (enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.updateRotation = false;
					enemy.Agent.isStopped = true;
				}

				Vector3 direction = (aggroTarget.transform.position - enemy.transform.position).normalized;
				direction.y = 0f;
				if (direction != Vector3.zero)
					enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, Quaternion.LookRotation(direction), 720f * deltaTime);
			}
			else
			{
				if (enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.updateRotation = true;
					enemy.Agent.isStopped = false;
				}

				Vector3 target = aggroTarget.transform.position;
				if ((!enemy.LastDestination.HasValue || Vector3.Distance(enemy.LastDestination.Value, target) > 0.5f) && enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.SetDestination(target);
					enemy.LastDestination = target;
				}
			}
		}

		private void HandleEscapingGroundEnemy(EnemyUnit enemy, Entity entity, PlayerModel player, float deltaTime)
		{
			if (stunStash.Has(entity))
			{
				enemy.Agent.updateRotation = false;
				enemy.Agent.isStopped = true;
				return;
			}

			if (player.Unit.IsSleeping || !player.CanMove || waitingMarkerStash.Has(entity))
			{
				if (enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.isStopped = true;
					enemy.Agent.updateRotation = false;
				}

				return;
			}

			ref EnemyCanEscapeMarker escape = ref escapeMarkerStash.Get(entity);
			float distance = Distance(enemy, PlayerUnit);
			escape.TimeSinceLastThreat = distance > (float)enemy.GetStatValue(StatType.AttackRange) ? escape.TimeSinceLastThreat + deltaTime : 0f;
			float timeSinceThreat = escape.TimeSinceLastThreat;
			float cooldown = escape.ThreatCooldown;
			if (enemy.LastWanderDestination.HasValue && enemy.Agent.isOnNavMesh && !enemy.Agent.pathPending && enemy.Agent.remainingDistance <= 0.5f)
				enemy.LastWanderDestination = null;
			if (enemy.Agent.isOnNavMesh && enemy.Agent.isStopped && enemy.LastWanderDestination.HasValue)
			{
				enemy.Agent.updateRotation = true;
				enemy.Agent.isStopped = false;
			}

			if (enemy.LastWanderDestination.HasValue)
				return;
			Vector3 target;
			if (timeSinceThreat >= cooldown)
			{
				Vector3 random = UnityEngine.Random.onUnitSphere;
				random.y = 0f;
				Vector3 sample = enemy.PatrolCenter + random.normalized * escape.EscapeDistance;
				if (!NavMesh.SamplePosition(sample, out NavMeshHit hit, escape.EscapeDistance, NavMesh.AllAreas))
					return;
				target = hit.position;
			}
			else if (UnityEngine.Random.value < 0.2f && lichSkeletonSystem.TryGetSkeletonCentroid(entity, out Vector3 centroid))
			{
				escape.StuckTimer = 0f;
				target = centroid;
			}
			else
			{
				Vector3 position = enemy.transform.position;
				Vector3 direction = position - PlayerUnit.transform.position;
				direction.y = 0f;
				target = position + direction.normalized * escape.EscapeDistance;
				NavMesh.CalculatePath(position, target, NavMesh.AllAreas, path);
				bool valid = path.status == NavMeshPathStatus.PathComplete;
				if (!valid && path.status == NavMeshPathStatus.PathPartial && path.corners.Length != 0)
				{
					target = path.corners[path.corners.Length - 1];
					valid = Vector3.Distance(position, target) > 0.5f;
				}

				if (valid)
					escape.StuckTimer = 0f;
				else
				{
					escape.StuckTimer += deltaTime;
					if (escape.StuckTimer < escape.StuckTimerMax)
						return;
					escape.StuckTimer = 0f;
					target = enemy.PatrolCenter;
				}
			}

			if (enemy.Agent.isOnNavMesh)
			{
				enemy.Agent.speed = (float)enemy.GetStatValue(StatType.MovementSpeed);
				enemy.Agent.updateRotation = true;
				enemy.Agent.isStopped = false;
				enemy.Agent.SetDestination(target);
			}

			enemy.LastWanderDestination = target;
		}

		private void HandleFlyingEnemy(EnemyUnit enemy, Entity entity, PlayerModel player, float deltaTime)
		{
			if (FriendlyDebuffs.IsDisplaced(enemy) || FriendlyDebuffs.IsChanneling(enemy))
			{
				if (enemy.Agent != null && enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
				{
					enemy.Agent.isStopped = true;
					enemy.Agent.updateRotation = false;
				}
				return;
			}
			if (escapeMarkerStash.Has(entity))
			{
				HandleEscapingFlyingEnemy(enemy, entity, player, deltaTime);
				return;
			}

			// Skills: casting holds flying enemies in place too (ground enemies already check IsCasting).
			if (stunStash.Has(entity) || enemy.IsCasting)
				return;
			enemy.RetreatTimer -= deltaTime;
			if (enemy.AttackCooldown > 0f)
				enemy.AttackCooldown -= deltaTime;
			double movementSpeed = enemy.GetStatValue(StatType.MovementSpeed);
			double attackRange = GetAttackRange(enemy);
			float playerDistance = Distance(enemy, PlayerUnit);
			// Pet mode: as for ground enemies, the nearest candidate is fought and the 30 m reset stays player-based.
			UnitCore aggroTarget = SelectAggroTarget(enemy, playerDistance, false, out float distance);
			if (playerDistance > 30f && !enemy.IsDead && !bossArenaUnitStash.Has(entity))
				enemy.CurrentHealth = enemy.GetStatValue(StatType.Health);
			bool playerUnavailable = player.Unit.IsSleeping || !player.CanMove || player.Unit.IsDying;
			bool retreat = (distance > enemy.AggroRange && enemy.AggroTimer <= 0f && !IsTargetedByAllies(enemy)) || enemy.RetreatTimer > 0f;
			Vector3 target;
			if (retreat || playerUnavailable || waitingMarkerStash.Has(entity))
			{
				if (enemy.InCombat)
				{
					enemy.IsReturningHome = !bossArenaUnitStash.Has(entity);
					enemy.InCombat = false;
				}
				ClearAggroTarget(enemy);
				target = enemy.CurrentSpawnerPatrolPoint;
			}
			else
			{
				enemy.InCombat = true;
				enemy.IsReturningHome = false;
				enemy.AggroTimer -= deltaTime;
				if (distance < enemy.AggroRange)
					enemy.AggroTimer = enemy.AggroDuration;
				target = attackRange > distance + 0.5f ? enemy.transform.position : aggroTarget.transform.position;
			}

			Vector3 position = enemy.transform.position;
			float remaining = Vector3.Distance(position, target);
			if (remaining > 0.1f)
			{
				Vector3 direction = (target - position).normalized;
				float step = (enemy.RetreatTimer > 0f ? 2f : 1f) * (float)movementSpeed * deltaTime;
				enemy.transform.position = step > remaining ? target : position + direction * step;
				direction.y = 0f;
				if (direction != Vector3.zero)
					enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, Quaternion.LookRotation(direction), 360f * deltaTime);
			}

			if (enemy.IsReturningHome && Vector3.Distance(enemy.transform.position, target) <= 0.1f)
				enemy.IsReturningHome = false;
			enemy.LastDestination = target;
			if (distance < attackRange)
				Attack(enemy);
		}

		private void HandleEscapingFlyingEnemy(EnemyUnit enemy, Entity entity, PlayerModel player, float deltaTime)
		{
			if (stunStash.Has(entity))
				return;
			float distance = Distance(enemy, PlayerUnit);
			float attackRange = (float)enemy.GetStatValue(StatType.AttackRange);
			bool keepDestination = player.Unit.IsSleeping || distance > attackRange || !player.CanMove || waitingMarkerStash.Has(entity);
			ref EnemyCanEscapeMarker escape = ref escapeMarkerStash.Get(entity);
			double movementSpeed = enemy.GetStatValue(StatType.MovementSpeed);
			Vector3 position = enemy.transform.position;
			if (!keepDestination)
			{
				Vector3 direction = position - PlayerUnit.transform.position;
				direction.y = 0f;
				Vector3 target = position + direction.normalized * escape.EscapeDistance;
				if (!enemy.LastWanderDestination.HasValue || Vector3.Distance(enemy.LastWanderDestination.Value, target) > 1f)
					enemy.LastWanderDestination = target;
			}
			else if (!enemy.LastWanderDestination.HasValue)
				return;
			Vector3 destination = enemy.LastWanderDestination.Value;
			float remaining = Vector3.Distance(position, destination);
			if (remaining <= 0.1f)
			{
				enemy.LastWanderDestination = null;
				return;
			}

			Vector3 moveDirection = (destination - position).normalized;
			float step = (float)movementSpeed * deltaTime;
			enemy.transform.position = step > remaining ? destination : position + moveDirection * step;
			moveDirection.y = 0f;
			if (moveDirection != Vector3.zero)
				enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, Quaternion.LookRotation(moveDirection), 360f * deltaTime);
		}

		private void Attack(EnemyUnit enemy)
		{
			if (enemy.AttackCooldown > 0f)
				return;
			// Pet mode: the hit goes to the unit this enemy is fighting (player or pet).
			UnitCore target = PetConfig.Enabled && enemy.AggroTarget != null ? enemy.AggroTarget : playerModel.Unit;
			// Ranged enemies: the basic attack is a projectile; its impact raises the same hit (no melee fallback).
			if (enemy.Ranged != null)
			{
				if (skillService == null && GameContext.Has<SkillService>())
					skillService = GameContext.Get<SkillService>();
				skillService?.LaunchRangedAttack(enemy, target, enemy.Ranged);
				enemy.AttackCooldown = (float)enemy.GetStatValue(StatType.AttackCooldown);
				return;
			}

			world.GetEvent<WeaponHitEvent>().NextFrame(new WeaponHitEvent { Source = enemy, Target = target, DamageType = enemy.DamageType });
			enemy.AttackCooldown = (float)enemy.GetStatValue(StatType.AttackCooldown);
		}

		// Ranged enemies shoot from max(AttackRange stat, RangedAttack.AttackRange); stat modifiers (Berserker) still apply above it.
		private static float GetAttackRange(EnemyUnit enemy)
		{
			float range = (float)enemy.GetStatValue(StatType.AttackRange);
			return enemy.Ranged != null ? Mathf.Max(range, enemy.Ranged.AttackRange) : range;
		}

		// Ranged ground enemy in combat: approach until in range with a clear line of fire, then hold and shoot;
		// optionally step back once per KiteCooldown when the target comes inside MinRange.
		private void HandleRangedGroundCombat(EnemyUnit enemy, UnitCore target, float distanceSquared, float attackRange, float deltaTime)
		{
			RangedAttackDef ranged = enemy.Ranged;
			NavMeshAgent agent = enemy.Agent;
			enemy.RangedKiteCooldown -= deltaTime;
			if (enemy.RangedKiteTimer > 0f)
			{
				enemy.RangedKiteTimer -= deltaTime;
				bool arrived = !agent.isOnNavMesh || !agent.pathPending && agent.remainingDistance < 0.2f;
				if (enemy.RangedKiteTimer > 0f && !arrived)
				{
					// A stun or cast during the step-back halted the agent; resume the step-back once it ends.
					if (agent.isOnNavMesh)
					{
						agent.updateRotation = true;
						agent.isStopped = false;
					}

					return;
				}

				enemy.RangedKiteTimer = 0f;
			}

			float holdRange = enemy.RangedHolding ? attackRange + RangedHoldSlack : attackRange;
			bool clear = distanceSquared < holdRange * holdRange && HasLineOfFire(enemy, target, distanceSquared, deltaTime);
			if (!clear)
			{
				enemy.RangedHolding = false;
				// Out of range: the next in-range frame raycasts afresh instead of reusing an old result.
				if (distanceSquared >= holdRange * holdRange)
					enemy.LineOfFireTimer = 0f;
				ChaseTarget(enemy, target);
				return;
			}

			if (ranged.MinRange > 0f && distanceSquared < ranged.MinRange * ranged.MinRange && enemy.RangedKiteCooldown <= 0f && TryStepBack(enemy, target, ranged))
			{
				enemy.RangedHolding = false;
				return;
			}

			enemy.RangedHolding = true;
			Attack(enemy);
			if (agent.isOnNavMesh)
			{
				agent.updateRotation = false;
				agent.isStopped = true;
			}

			Vector3 direction = target.transform.position - enemy.transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0.0001f)
				enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, Quaternion.LookRotation(direction.normalized), 720f * deltaTime);
		}

		private static void ChaseTarget(EnemyUnit enemy, UnitCore target)
		{
			NavMeshAgent agent = enemy.Agent;
			if (agent.isOnNavMesh)
			{
				agent.updateRotation = true;
				agent.isStopped = false;
			}

			Vector3 destination = target.transform.position;
			if ((!enemy.LastDestination.HasValue || Vector3.Distance(enemy.LastDestination.Value, destination) > 0.5f) && agent.enabled && agent.isOnNavMesh)
			{
				agent.SetDestination(destination);
				enemy.LastDestination = destination;
			}
		}

		// Walls cut the navmesh, so a navmesh raycast is a cheap line-of-fire test; cached per unit for a short interval.
		private static bool HasLineOfFire(EnemyUnit enemy, UnitCore target, float distanceSquared, float deltaTime)
		{
			if (!enemy.Ranged.RequireLineOfFire || distanceSquared < LineOfFireIgnoreDistance * LineOfFireIgnoreDistance || !enemy.Agent.isOnNavMesh)
				return true;
			enemy.LineOfFireTimer -= deltaTime;
			// The cached result belongs to one target; a target switch (pet mode) refreshes it at once.
			if (enemy.LineOfFireTimer > 0f && enemy.LineOfFireTarget == target)
				return enemy.HasLineOfFire;
			enemy.LineOfFireTimer = LineOfFireInterval;
			enemy.LineOfFireTarget = target;
			enemy.HasLineOfFire = !NavMesh.Raycast(enemy.transform.position, target.transform.position, out _, NavMesh.AllAreas);
			return enemy.HasLineOfFire;
		}

		private static bool TryStepBack(EnemyUnit enemy, UnitCore target, RangedAttackDef ranged)
		{
			NavMeshAgent agent = enemy.Agent;
			enemy.RangedKiteCooldown = 0.5f;
			if (!agent.enabled || !agent.isOnNavMesh || ranged.KiteDistance <= 0.1f)
				return false;
			Vector3 position = enemy.transform.position;
			Vector3 away = position - target.transform.position;
			away.y = 0f;
			if (away.sqrMagnitude < 0.0001f)
				away = -enemy.transform.forward;
			Vector3 desired = position + away.normalized * ranged.KiteDistance;
			if (!NavMesh.SamplePosition(desired, out NavMeshHit sample, 1.5f, NavMesh.AllAreas))
				return false;
			Vector3 destination = sample.position;
			// Stop at the first navmesh edge (cliff, wall) on the way back.
			if (NavMesh.Raycast(position, destination, out NavMeshHit edge, NavMesh.AllAreas))
				destination = edge.position;
			if ((destination - position).sqrMagnitude < 1f)
				return false;
			agent.updateRotation = true;
			agent.isStopped = false;
			agent.SetDestination(destination);
			enemy.LastDestination = destination;
			float speed = Mathf.Max(0.5f, agent.speed);
			enemy.RangedKiteTimer = Mathf.Clamp(ranged.KiteDistance / speed + 0.3f, 0.4f, 2.5f);
			enemy.RangedKiteCooldown = Mathf.Max(0.5f, ranged.KiteCooldown);
			return true;
		}

		// Original: always the player. Pet mode: the nearest of the player and the living pets, with its distance
		// (squared for ground enemies); the result is stored in enemy.AggroTarget.
		private UnitCore SelectAggroTarget(EnemyUnit enemy, float playerDistance, bool squared, out float distance)
		{
			distance = playerDistance;
			if (!PetConfig.Enabled || petsModel == null)
				return PlayerUnit;
			UnitCore best = PlayerUnit;
			foreach (PetModel pet in petsModel.Pets)
			{
				if (!pet.IsAlive || pet.Unit.IsDead)
					continue;
				float petDistance = squared ? SqrDistance(enemy, pet.Unit) : Distance(enemy, pet.Unit);
				if (petDistance < distance)
				{
					distance = petDistance;
					best = pet.Unit;
				}
			}
			enemy.AggroTarget = best;
			return best;
		}

		// Original: only the player's current target keeps aggro. Pet mode: any pet's target does too.
		private bool IsTargetedByAllies(EnemyUnit enemy)
		{
			if (playerModel.Unit.CurrentTarget == enemy)
				return true;
			if (!PetConfig.Enabled || petsModel == null)
				return false;
			foreach (PetModel pet in petsModel.Pets)
				if (pet.IsAlive && pet.Unit.CurrentTarget == enemy)
					return true;
			return false;
		}

		private static void ClearAggroTarget(EnemyUnit enemy)
		{
			if (PetConfig.Enabled)
				enemy.AggroTarget = null;
		}

		private static float Distance(UnitCore u1, UnitCore u2)
		{
			return Vector3.Distance(u1.transform.position, u2.transform.position);
		}

		private static float SqrDistance(UnitCore u1, UnitCore u2)
		{
			return (u1.transform.position - u2.transform.position).sqrMagnitude;
		}
	}
}
