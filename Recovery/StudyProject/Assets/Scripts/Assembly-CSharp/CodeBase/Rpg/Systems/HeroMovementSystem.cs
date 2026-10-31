using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Input;
using Codebase.Player.Models;
using Codebase.Skills;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Rpg.Systems
{
	public class HeroMovementSystem : SystemBase
	{
		private const float PathFailTeleportTime = 2f;
		private const float WalkingVelocitySqr = 0.01f;
		private const float StuckFollowDistanceSqr = (PetsModel.FollowRadius + 1.5f) * (PetsModel.FollowRadius + 1.5f);
		private const float OwnerWarpDistanceSqr = 4f * 4f;

		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private SkillService skillService;
		private HeroUnit lastOwner;
		private Vector3 lastOwnerPosition;
		private HeroUnit visualOwner;
		private EnemyUnit visualTarget;

		public override void OnAwake() { }

		public override void OnUpdate(float deltaTime)
		{
			PlayerUnit player = playerModel.Unit;
			HeroUnit owner = RpgControl.ControlledUnit(playerModel) as HeroUnit;
			if (player == null || owner == null) return;
			Vector3 ownerPosition = owner.transform.position;
			bool ownerWarped = lastOwner == owner && HeroAi.FlatSqrDistance(ownerPosition, lastOwnerPosition) > OwnerWarpDistanceSqr;
			lastOwner = owner;
			lastOwnerPosition = ownerPosition;
			foreach (HeroModel hero in roster.Heroes)
			{
				HeroUnit unit = hero.Unit;
				if (unit == null || !hero.IsAlive) { hero.PathFailTimer = 0f; continue; }
				if (unit.IsControlled) MoveControlled(unit, deltaTime);
				else MoveFollower(hero, owner, ownerPosition, ownerWarped, deltaTime);
			}
		}

		private void MoveControlled(HeroUnit unit, float deltaTime)
		{
			NavMeshAgent agent = unit.Agent;
			if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
			if (FriendlyDebuffs.IsStunned(unit) || FriendlyDebuffs.IsDisplaced(unit) || FriendlyDebuffs.IsChanneling(unit))
			{
				unit.SetWalking(false);
				return;
			}
			Vector2 input = GameContext.Get<Joystick>().GetInput();
			float speed = playerModel.CanMove ? (float)unit.GetStatSpecificValue(StatType.MovementSpeed) : 0f;
			// Player movement buffs are already applied to the stat; companions read the skill factor separately.
			if (!(unit is PlayerUnit)) speed *= skillService.GetMoveSpeedFactor(unit);
			UnityEngine.Camera camera = UnityEngine.Camera.main;
			Vector3 forward = camera != null ? (unit is PlayerUnit ? unit.transform.position - camera.transform.position : camera.transform.forward) : Vector3.forward;
			forward.y = 0f;
			forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
			Vector3 movement = Quaternion.LookRotation(forward) * new Vector3(input.x, 0f, input.y) * speed * deltaTime;
			agent.Move(movement);
			if (visualOwner != unit) { visualOwner = unit; visualTarget = null; }
			if (unit.CurrentTarget != null && !unit.CurrentTarget.IsDead) visualTarget = unit.CurrentTarget;
			else
			{
				EnemyUnit nearest = null;
				float nearestSqr = float.MaxValue;
				foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					float sqr = (enemy.transform.position - unit.transform.position).sqrMagnitude;
					if (sqr < nearestSqr) { nearest = enemy; nearestSqr = sqr; }
				}
				visualTarget = nearest != null && (visualTarget != null ? nearestSqr <= 36f : nearestSqr < 16f) ? nearest : null;
			}
			UnitMovement movementView = unit.GetComponent<UnitMovement>();
			if (movementView != null) movementView.VisualTarget = visualTarget;
			Vector3 direction = visualTarget != null && !visualTarget.IsDead ? visualTarget.transform.position - unit.transform.position : movement;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0f) unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, Quaternion.LookRotation(direction), 460f * deltaTime);
			unit.SetWalking(playerModel.CanMove && input.sqrMagnitude > 0f);
		}

		private void MoveFollower(HeroModel hero, HeroUnit owner, Vector3 ownerPosition, bool ownerWarped, float deltaTime)
		{
			HeroUnit unit = hero.Unit;
			NavMeshAgent agent = unit.Agent;
			if (agent == null || !agent.enabled) return;
			bool paused = !playerModel.CanMove || playerModel.Unit.IsSleeping;
			if (hero.IsProtagonist && roster.InWipe)
			{
				hero.AiTarget = null;
				Stop(agent, unit);
				return;
			}
			if (skillService.IsDisplaced(unit)) { hero.PathFailTimer = 0f; unit.SetWalking(false); return; }
			PetConfig config = PetConfig.Instance;
			float distanceSqr = HeroAi.FlatSqrDistance(unit.transform.position, ownerPosition);
			float teleportSqr = config.TeleportDistance * config.TeleportDistance;
			if (skillService.IsStunned(unit) || skillService.IsCasting(unit))
			{
				if (ownerWarped && distanceSqr > teleportSqr)
				{
					if (skillService.IsStunned(unit)) skillService.ClearStun(unit);
					if (skillService.IsCasting(unit)) skillService.CancelCast(unit);
				}
				else { Stop(agent, unit); hero.PathFailTimer = 0f; return; }
			}
			EnemyUnit target = hero.IsProtagonist ? hero.AiTarget : unit.CurrentTarget;
			if (RpgControl.ShouldTeleport(!agent.isOnNavMesh, distanceSqr > teleportSqr, hero.PathFailTimer >= PathFailTeleportTime && target == null))
			{
				unit.CurrentTarget = null;
				hero.AiTarget = null;
				PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(ownerPosition, owner.transform.eulerAngles.y, hero.IsProtagonist ? 0 : hero.Pet.SlotOrder), ownerPosition, out Vector3 position);
				if (agent.Warp(position) && agent.isOnNavMesh) agent.ResetPath();
				hero.PathFailTimer = 0f;
				unit.SetWalking(false);
				return;
			}
			if (hero.IsProtagonist) target = HeroAi.SelectFollowerTarget(unit, owner, config.EngageRadius * config.EngageRadius, target);
			if (paused || distanceSqr > config.LeashRadius * config.LeashRadius || target == null || target.IsDead || hero.PathFailTimer >= PathFailTeleportTime)
			{
				if (target != null) hero.PathFailTimer = 0f;
				target = null;
			}
			hero.AiTarget = target;
			unit.CurrentTarget = target;
			float ownSpeed = (float)unit.GetStatSpecificValue(StatType.MovementSpeed);
			if (hero.IsProtagonist) agent.speed = ownSpeed * (target == null ? config.FollowSpeedMultiplier : 1f);
			else
			{
				float ownerSpeed = (float)owner.GetStatSpecificValue(StatType.MovementSpeed);
				agent.speed = (target != null ? ownSpeed : Mathf.Max(ownSpeed, ownerSpeed) * config.FollowSpeedMultiplier) * skillService.GetMoveSpeedFactor(unit);
			}
			bool moving = target != null ? HeroAi.Chase(unit, agent, target, deltaTime) : HeroAi.MoveTo(agent, PetsModel.GetFollowPosition(ownerPosition, owner.transform.eulerAngles.y, hero.IsProtagonist ? 0 : hero.Pet.SlotOrder)) && distanceSqr > StuckFollowDistanceSqr;
			bool failing = moving && !agent.pathPending && (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.velocity.sqrMagnitude <= WalkingVelocitySqr);
			hero.PathFailTimer = failing ? hero.PathFailTimer + deltaTime : 0f;
			unit.SetWalking(agent.velocity.sqrMagnitude > WalkingVelocitySqr);
		}

		private static void Stop(NavMeshAgent agent, HeroUnit unit)
		{
			if (agent.isOnNavMesh)
			{
				if (agent.hasPath) agent.ResetPath();
				agent.velocity = Vector3.zero;
			}
			unit.SetWalking(false);
		}
	}
}
