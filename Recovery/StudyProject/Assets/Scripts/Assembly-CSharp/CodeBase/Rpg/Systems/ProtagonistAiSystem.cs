using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Skills;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Rpg.Systems
{
	public class ProtagonistAiSystem : SystemBase
	{
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private SkillService skillService;

		public override void OnAwake() { }

		public override void OnUpdate(float deltaTime)
		{
			if (!RpgModeConfig.Enabled) return;
			PlayerUnit unit = playerModel.Unit;
			HeroModel hero = roster.Get(unit);
			HeroUnit owner = RpgControl.ControlledUnit(playerModel) as HeroUnit;
			if (unit == null || hero == null || owner == null || unit.IsControlled || unit.IsDead || unit.IsDying || unit.IsReviving) return;
			NavMeshAgent agent = unit.Agent;
			if (agent == null || !agent.enabled) return;
			if (!playerModel.CanMove || unit.IsSleeping)
			{
				hero.AiTarget = null;
				if (agent.isOnNavMesh)
				{
					if (agent.hasPath) agent.ResetPath();
					agent.velocity = Vector3.zero;
				}
				return;
			}
			if (skillService.IsDisplaced(unit)) return;
			if (skillService.IsStunned(unit) || skillService.IsCasting(unit))
			{
				if (agent.isOnNavMesh)
				{
					if (agent.hasPath) agent.ResetPath();
					agent.velocity = Vector3.zero;
				}
				hero.PathFailTimer = 0f;
				return;
			}
			PetConfig config = PetConfig.Instance;
			Vector3 ownerPosition = owner.transform.position;
			float distanceSqr = HeroAi.FlatSqrDistance(unit.transform.position, ownerPosition);
			if (!agent.isOnNavMesh || Time.time >= roster.GraceUntil && distanceSqr > config.TeleportDistance * config.TeleportDistance || hero.PathFailTimer >= 2f && hero.AiTarget == null)
			{
				PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(ownerPosition, owner.transform.eulerAngles.y, 0), ownerPosition, out Vector3 position);
				if (agent.Warp(position)) agent.ResetPath();
				hero.PathFailTimer = 0f;
				hero.AiTarget = null;
				return;
			}
			float engage = config.EngageRadius;
			hero.AiTarget = HeroAi.SelectFollowerTarget(unit, owner, engage * engage, hero.AiTarget);
			if (distanceSqr > config.LeashRadius * config.LeashRadius || hero.PathFailTimer >= 2f)
			{
				hero.AiTarget = null;
				hero.PathFailTimer = 0f;
			}
			agent.speed = (float)unit.GetStatSpecificValue(StatType.MovementSpeed) * (hero.AiTarget == null ? config.FollowSpeedMultiplier : 1f);
			bool moving;
			if (hero.AiTarget != null)
			{
				float range = (float)unit.GetStatValue(StatType.AttackRange);
				Vector3 direction = hero.AiTarget.transform.position - unit.transform.position;
				if (direction.sqrMagnitude < range * range)
				{
					unit.CurrentTarget = hero.AiTarget;
					if (agent.hasPath) agent.ResetPath();
					direction.y = 0f;
					if (direction.sqrMagnitude > 0.0001f)
						unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, Quaternion.LookRotation(direction), 720f * deltaTime);
					moving = false;
				}
				else
				{
					agent.SetDestination(hero.AiTarget.transform.position);
					moving = true;
				}
			}
			else moving = HeroAi.MoveTo(agent, PetsModel.GetFollowPosition(ownerPosition, owner.transform.eulerAngles.y, 0)) && distanceSqr > (PetsModel.FollowRadius + 1.5f) * (PetsModel.FollowRadius + 1.5f);
			bool failing = moving && !agent.pathPending && (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.velocity.sqrMagnitude <= 0.01f);
			hero.PathFailTimer = failing ? hero.PathFailTimer + deltaTime : 0f;
		}
	}
}
