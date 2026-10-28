using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
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
			float attackRange = (float)enemy.GetStatValue(StatType.AttackRange);
			float playerDistanceSquared = SqrDistance(enemy, PlayerUnit);
			// Pet mode: the nearest of the player and the living pets is fought; the 30 m reset stays player-based.
			UnitCore aggroTarget = SelectAggroTarget(enemy, playerDistanceSquared, true, out float distanceSquared);
			if (playerDistanceSquared > 900f && !enemy.IsDead && !bossArenaUnitStash.Has(entity))
				enemy.CurrentHealth = enemy.GetStatValue(StatType.Health);
			bool playerUnavailable = player.Unit.IsSleeping || !player.CanMove || player.Unit.IsDying;
			bool retreat = (distanceSquared > enemy.AggroRange * enemy.AggroRange && enemy.AggroTimer <= 0f && !IsTargetedByAllies(enemy)) || enemy.RetreatTimer > 0f;
			if (retreat || playerUnavailable || waitingMarkerStash.Has(entity))
			{
				ClearAggroTarget(enemy);
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
				}

				return;
			}

			enemy.AggroTimer -= deltaTime;
			if (distanceSquared < enemy.AggroRange * enemy.AggroRange)
				enemy.AggroTimer = enemy.AggroDuration;
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
			if (escapeMarkerStash.Has(entity))
			{
				HandleEscapingFlyingEnemy(enemy, entity, player, deltaTime);
				return;
			}

			if (stunStash.Has(entity))
				return;
			enemy.RetreatTimer -= deltaTime;
			if (enemy.AttackCooldown > 0f)
				enemy.AttackCooldown -= deltaTime;
			double movementSpeed = enemy.GetStatValue(StatType.MovementSpeed);
			double attackRange = enemy.GetStatValue(StatType.AttackRange);
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
				ClearAggroTarget(enemy);
				target = enemy.CurrentSpawnerPatrolPoint;
			}
			else
			{
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
			world.GetEvent<WeaponHitEvent>().NextFrame(new WeaponHitEvent { Source = enemy, Target = target, DamageType = enemy.DamageType });
			enemy.AttackCooldown = (float)enemy.GetStatValue(StatType.AttackCooldown);
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
