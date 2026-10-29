using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Pet.Systems
{
	// Follow ring, chase, leash and teleport for living pets.
	public class PetMovementSystem : SystemBase
	{
		private const float PathFailTeleportTime = 2f;

		private const float DestinationTolerance = 0.5f;

		private const float ArriveDistance = 0.3f;

		private const float TurnSpeed = 720f;

		private const float WalkingVelocitySqr = 0.01f;

		// Being stuck this close to the player (e.g. ring slot inside a wall) is not a path failure.
		private const float StuckFollowDistanceSqr = (PetsModel.FollowRadius + 1.5f) * (PetsModel.FollowRadius + 1.5f);

		// A hero jump this far in one frame is a warp (arena, revive, gate), not walking.
		private const float OwnerWarpDistanceSqr = 4f * 4f;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PetsModel petsModel;

		[Inject]
		private global::Codebase.Skills.SkillService skillService;

		private UnitCore lastOwner;

		private Vector3 lastOwnerPosition;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!PetConfig.Enabled)
				return;
			PlayerUnit player = playerModel.Unit;
			if (player == null)
				return;
			PetConfig config = PetConfig.Instance;
			bool paused = !playerModel.CanMove || player.IsDying || player.IsSleeping;
			float playerSpeed = (float)player.GetStatSpecificValue(StatType.MovementSpeed);
			// Skill slows on the hero are its own (felt speed scales by exactly that factor); pets only take their own below.
			float heroSkillFactor = skillService.GetMoveSpeedFactor(player);
			if (heroSkillFactor > 0.01f)
				playerSpeed /= heroSkillFactor;
			// Following keeps up with the player; only chasing a target uses the pet's move speed ratio.
			float followSpeed = playerSpeed * config.FollowSpeedMultiplier;
			Transform owner = global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel).transform;
			Vector3 ownerPosition = owner.position;
			bool ownerWarped = lastOwner == global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel) && FlatSqrDistance(ownerPosition, lastOwnerPosition) > OwnerWarpDistanceSqr;
			lastOwner = global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel);
			lastOwnerPosition = ownerPosition;
			float leashSqr = config.LeashRadius * config.LeashRadius;
			float teleportSqr = config.TeleportDistance * config.TeleportDistance;
			foreach (PetModel pet in petsModel.Pets)
			{
				if (!pet.IsAlive)
					continue;
				PetUnit unit = pet.Unit;
				if (global::Codebase.Rpg.RpgModeConfig.Enabled && unit.IsControlled) continue;
				if (skillService.IsDisplaced(unit))
				{
					pet.PathFailTimer = 0f;
					unit.SetWalking(false);
					continue;
				}
				NavMeshAgent agent = unit.Agent;
				if (agent == null || !agent.enabled)
					continue;
				Vector3 position = unit.transform.position;
				float ownerDistanceSqr = FlatSqrDistance(position, ownerPosition);
				if (skillService.IsStunned(unit))
				{
					// The hero warped away (arena, revive, gate): drop the stun and teleport as usual. Walking away does not.
					if (ownerWarped && ownerDistanceSqr > teleportSqr)
						skillService.ClearStun(unit);
					else
					{
						// Stunned: no pathing, no walking, no path-fail teleport; the target is kept for afterwards.
						if (agent.isOnNavMesh)
						{
							if (agent.hasPath)
								agent.ResetPath();
							agent.velocity = Vector3.zero;
						}
						pet.PathFailTimer = 0f;
						unit.SetWalking(false);
						continue;
					}
				}
				if (skillService.IsCasting(unit))
				{
					if (ownerWarped && ownerDistanceSqr > teleportSqr)
						skillService.CancelCast(unit);
					else
					{
						if (agent.isOnNavMesh)
						{
							if (agent.hasPath)
								agent.ResetPath();
							agent.velocity = Vector3.zero;
						}
						pet.PathFailTimer = 0f;
						unit.SetWalking(false);
						continue;
					}
				}
				if (global::Codebase.Rpg.RpgControl.ShouldTeleport(!agent.isOnNavMesh, ownerDistanceSqr > teleportSqr, pet.PathFailTimer >= PathFailTeleportTime && unit.CurrentTarget == null))
				{
					unit.CurrentTarget = null;
					petsModel.Warp(pet, ownerPosition, owner.eulerAngles.y);
					unit.SetWalking(false);
					continue;
				}

				EnemyUnit target = unit.CurrentTarget;
				// A target that cannot be reached for a while is dropped instead of teleporting.
				if (paused || ownerDistanceSqr > leashSqr || target == null || target.IsDead || pet.PathFailTimer >= PathFailTeleportTime)
				{
					if (target != null)
						pet.PathFailTimer = 0f;
					unit.CurrentTarget = null;
					target = null;
				}

				agent.speed = target != null ? playerSpeed * PetStats.GetRatios(pet.Item, pet.Definition).MoveSpeed : followSpeed;
				if (global::Codebase.Rpg.RpgModeConfig.Enabled)
				{
					float ownSpeed = (float)unit.GetStatSpecificValue(StatType.MovementSpeed);
					float ownerSpeed = (float)global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel).GetStatSpecificValue(StatType.MovementSpeed);
					agent.speed = target != null ? ownSpeed : Mathf.Max(ownSpeed, ownerSpeed) * config.FollowSpeedMultiplier;
				}
				// Skills: pets move off the hero's speed, so skill slows on the pet itself apply here.
				agent.speed *= skillService.GetMoveSpeedFactor(unit);
				bool moving;
				if (target != null)
					moving = Chase(unit, agent, target, deltaTime);
				else
					moving = MoveTo(agent, PetsModel.GetFollowPosition(ownerPosition, owner.eulerAngles.y, pet.SlotOrder)) && ownerDistanceSqr > StuckFollowDistanceSqr;
				bool failing = moving && !agent.pathPending && (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.velocity.sqrMagnitude <= WalkingVelocitySqr);
				pet.PathFailTimer = failing ? pet.PathFailTimer + deltaTime : 0f;
				unit.SetWalking(agent.velocity.sqrMagnitude > WalkingVelocitySqr);
			}
		}

		private static bool Chase(PetUnit unit, NavMeshAgent agent, EnemyUnit target, float deltaTime)
		{
			Vector3 position = unit.transform.position;
			Vector3 targetPosition = target.transform.position;
			float range = (float)unit.GetStatValue(StatType.AttackRange);
			if (FlatSqrDistance(position, targetPosition) > range * range)
				return MoveTo(agent, targetPosition);
			if (agent.hasPath)
				agent.ResetPath();
			Vector3 direction = targetPosition - position;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0.0001f)
				unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, Quaternion.LookRotation(direction), TurnSpeed * deltaTime);
			return false;
		}

		private static bool MoveTo(NavMeshAgent agent, Vector3 destination)
		{
			if (FlatSqrDistance(agent.transform.position, destination) <= ArriveDistance * ArriveDistance)
			{
				if (agent.hasPath)
					agent.ResetPath();
				return false;
			}

			if (!agent.hasPath && !agent.pathPending || FlatSqrDistance(agent.destination, destination) > DestinationTolerance * DestinationTolerance)
				agent.SetDestination(destination);
			return true;
		}

		private static float FlatSqrDistance(Vector3 a, Vector3 b)
		{
			float x = a.x - b.x;
			float z = a.z - b.z;
			return x * x + z * z;
		}
	}
}
