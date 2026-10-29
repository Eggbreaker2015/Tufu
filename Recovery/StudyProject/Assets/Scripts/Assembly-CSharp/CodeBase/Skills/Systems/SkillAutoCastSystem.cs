using Codebase.Core;
using Codebase.Enemy;
using Codebase.Enemy.Components;
using Codebase.Enemy.Systems;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Skills.Components;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Skills.Systems
{
	// Runs before EnemyCastSystem: pending missiles, cast progress (cast point -> effects -> backswing), cooldowns,
	// passive aura pulses and the AI think (every ThinkInterval, staggered per unit).
	public class SkillAutoCastSystem : SystemBase
	{
		private const float DefaultSearchRange = 15f;

		private const float DefaultDensityRadius = 3f;

		[Inject]
		private SkillService skillService;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EnemyCastSystem enemyCastSystem;

		[Inject]
		private Stash<SkillCasterComponent> casterStash;

		[Inject]
		private Stash<SkillCastingComponent> castingStash;

		[Inject]
		private Stash<BuffStunComponent> stunStash;

		[Inject]
		private Stash<UnitSpawnActiveMarker> spawnActiveStash;

		[Inject]
		private Stash<UnitWaitingAttackMarker> waitingStash;

		private Filter casterFilter;

		private Filter castingFilter;

		public override void OnAwake()
		{
			casterFilter = Filter.With<SkillCasterComponent>().Build();
			castingFilter = Filter.With<SkillCastingComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			skillService.TickMissiles(deltaTime);
			skillService.TickSummons();
			UpdateCasts(deltaTime);
			float now = Time.time;
			foreach (Entity entity in casterFilter)
			{
				ref SkillCasterComponent caster = ref casterStash.Get(entity);
				UnitCore unit = caster.Owner;
				if (unit == null || unit.IsDead || unit.IsFar || !unit.gameObject.activeInHierarchy)
					continue;
				bool engaged = IsEngaged(unit);
				if (engaged && !caster.Engaged)
					foreach (SkillSlot slot in caster.Slots)
						slot.Cooldown = Mathf.Max(slot.Cooldown, slot.Def.InitialCooldown);
				caster.Engaged = engaged;
				foreach (SkillSlot slot in caster.Slots)
				{
					if (engaged && slot.Cooldown > 0f)
						slot.Cooldown -= deltaTime;
					if (slot.Def.TargetType == SkillTargetType.Passive)
						PulsePassive(unit, slot, deltaTime);
				}
				if (!engaged || now < caster.NextThinkTime)
					continue;
				caster.NextThinkTime = now + SkillService.ThinkInterval;
				if (CanThink(entity, unit))
					Think(entity, caster, unit);
			}
		}

		private void PulsePassive(UnitCore unit, SkillSlot slot, float deltaTime)
		{
			slot.AuraTimer -= deltaTime;
			if (slot.AuraTimer > 0f)
				return;
			float interval = Mathf.Max(0.1f, SkillService.GetAuraInterval(slot.Def));
			slot.AuraTimer = Mathf.Max(slot.AuraTimer + interval, 0.01f);
			SkillCastContext context = skillService.CreateContext(unit, slot.Def, slot.Level, 1f, null, unit.transform.position, null);
			SkillService.Execute(slot.Def.Effects, context);
		}

		// Enemies only act on skills in combat (aggro window or targeted by the hero); anything else always.
		private bool IsEngaged(UnitCore unit)
		{
			if (!(unit is EnemyUnit enemy))
				return true;
			return enemy.AggroTimer > 0f || playerModel.Unit != null && playerModel.Unit.CurrentTarget == enemy;
		}

		private bool CanThink(Entity entity, UnitCore unit)
		{
			if (stunStash.Has(entity) || castingStash.Has(entity) || skillService.IsSilenced(entity))
				return false;
			if (unit is EnemyUnit enemy)
			{
				if (enemy.IsCasting || enemyCastSystem.IsCasting(entity) || spawnActiveStash.Has(entity) || waitingStash.Has(entity) || enemy.RetreatTimer > 0f)
					return false;
				PlayerUnit player = playerModel.Unit;
				if (player == null || player.IsDying || player.IsSleeping || !playerModel.CanMove)
					return false;
			}
			return true;
		}

		private void Think(Entity entity, in SkillCasterComponent caster, UnitCore unit)
		{
			SkillSlot best = null;
			UnitCore bestTarget = null;
			int bestPriority = int.MinValue;
			foreach (SkillSlot slot in caster.Slots)
			{
				SkillDef def = slot.Def;
				if (def.TargetType == SkillTargetType.Passive || slot.Cooldown > 0f || def.AutoCast == null)
					continue;
				if (best != null && def.AutoCast.Priority <= bestPriority)
					continue;
				UnitCore target = Pick(unit, def);
				if (target == null)
					continue;
				if (def.Range > 0f && target != unit && SkillTargetQuery.FlatDistance(unit, target) > def.Range)
					continue;
				SkillCastContext context = skillService.CreateContext(unit, def, slot.Level, 1f, target, GetPoint(unit, def, target), null);
				if (!SkillService.CheckConditions(def.AutoCast.UseWhen, context))
					continue;
				best = slot;
				bestTarget = target;
				bestPriority = def.AutoCast.Priority;
			}
			if (best != null)
				StartCast(entity, unit, best, bestTarget);
		}

		private UnitCore Pick(UnitCore unit, SkillDef def)
		{
			float range = def.Range > 0f ? def.Range : DefaultSearchRange;
			switch (def.AutoCast.Pick)
			{
				case SkillPick.Self:
					return unit;
				case SkillPick.NearestHostile:
					return SkillTargetQuery.Nearest(unit, SkillTargetFilter.Enemy | SkillTargetFilter.Alive, range);
				case SkillPick.LowestHpAlly:
					return SkillTargetQuery.LowestHealthFraction(unit, SkillTargetFilter.Ally | SkillTargetFilter.Self | SkillTargetFilter.Alive, range);
				case SkillPick.DensestHostile:
					return SkillTargetQuery.Densest(unit, def.Telegraph != null && def.Telegraph.Radius > 0f ? def.Telegraph.Radius : DefaultDensityRadius, range);
				case SkillPick.Hero:
					PlayerUnit hero = playerModel.Unit;
					if (SkillTargetQuery.IsAlive(hero) && !hero.IsDying && SkillTargetQuery.IsHostile(unit, hero) && SkillTargetQuery.FlatDistance(unit, hero) <= range)
						return hero;
					return GetCurrentTarget(unit);
				default:
					return GetCurrentTarget(unit);
			}
		}

		// Enemies: AggroTarget, else the hero (as EnemyAttackSystem.Attack).
		private UnitCore GetCurrentTarget(UnitCore unit)
		{
			UnitCore target;
			if (unit is EnemyUnit enemy)
				target = PetConfig.Enabled && enemy.AggroTarget != null ? enemy.AggroTarget : playerModel.Unit;
			else if (unit is PetUnit pet)
				target = pet.CurrentTarget;
			else if (unit is PlayerUnit player)
				target = player.CurrentTarget;
			else
				target = null;
			return SkillTargetQuery.IsAlive(target) && SkillTargetQuery.IsHostile(unit, target) ? target : null;
		}

		private static Vector3 GetPoint(UnitCore unit, SkillDef def, UnitCore target)
		{
			bool onTarget = target != null && (def.TargetType == SkillTargetType.Unit || def.TargetType == SkillTargetType.Point || def.Telegraph != null && def.Telegraph.AnchorOnTarget);
			return onTarget ? target.transform.position : unit.transform.position;
		}

		private void StartCast(Entity entity, UnitCore unit, SkillSlot slot, UnitCore target)
		{
			SkillDef def = slot.Def;
			slot.Cooldown = def.Cooldown;
			Vector3 direction = SkillTargetQuery.GetFlatDirection(unit, target);
			if (target != null && target != unit)
			{
				if (unit is EnemyUnit turningEnemy && turningEnemy.Agent != null && turningEnemy.Agent.enabled && turningEnemy.Agent.isOnNavMesh)
					turningEnemy.Agent.updateRotation = false;
				unit.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
			}
			Vector3 point = GetPoint(unit, def, target);
			float duration = Mathf.Max(0f, def.CastPoint) + Mathf.Max(0f, def.Backswing);
			if (duration <= 0f)
			{
				Fire(unit, slot, target, point, direction);
				return;
			}
			if (unit is EnemyUnit enemy)
			{
				// No cast animation: drop the walk/attack loop, EnemyUnit.Tick leaves the animator alone while casting.
				if (string.IsNullOrEmpty(def.AnimationBool) && string.IsNullOrEmpty(def.AnimationTrigger))
					enemy.StopLocomotionAnimation();
				enemyCastSystem.StartCast(entity, new EnemyCastingState
				{
					Duration = duration,
					AnimationBool = string.IsNullOrEmpty(def.AnimationBool) ? null : def.AnimationBool,
					AnimationTrigger = string.IsNullOrEmpty(def.AnimationTrigger) ? null : def.AnimationTrigger
				});
			}
			SkillTelegraphView telegraph = null;
			bool anchored = target != null && (def.TargetType == SkillTargetType.Point || def.Telegraph != null && def.Telegraph.AnchorOnTarget);
			// Hostile skills only; friendly skills are not drawn.
			if (def.CastPoint > 0f && unit is EnemyUnit && def.Telegraph != null)
			{
				TelegraphDef warning = def.Telegraph;
				switch (warning.Shape)
				{
					case TelegraphShape.Circle:
						telegraph = SkillTelegraphView.Spawn(point, warning.Radius, anchored ? null : unit.transform);
						break;
					case TelegraphShape.Line:
					case TelegraphShape.Cone:
						telegraph = SkillTelegraphView.Spawn(warning.Shape, unit.transform.position, direction, warning.Radius, warning.Angle > 0f ? warning.Angle : 90f, warning.Width > 0f ? warning.Width : 1f, unit.transform);
						break;
				}
			}
			castingStash.Set(entity, new SkillCastingComponent { Caster = unit, Slot = slot, Target = target, Point = point, Direction = direction, Elapsed = 0f, Fired = false, Telegraph = telegraph });
			if (def.CastPoint <= 0f)
			{
				ref SkillCastingComponent cast = ref castingStash.Get(entity);
				cast.Fired = true;
				Fire(unit, slot, target, point, direction);
			}
		}

		private void UpdateCasts(float deltaTime)
		{
			foreach (Entity entity in castingFilter)
			{
				ref SkillCastingComponent cast = ref castingStash.Get(entity);
				UnitCore unit = cast.Caster;
				if (unit == null || unit.IsDead || !unit.gameObject.activeInHierarchy || cast.Slot == null)
				{
					skillService.CancelCast(entity);
					continue;
				}
				SkillDef def = cast.Slot.Def;
				// Interrupted: stunned or silenced at any point before the cast point; the enemy cast state ends with it.
				if (!cast.Fired && (stunStash.Has(entity) || skillService.IsSilenced(entity)))
				{
					skillService.CancelCast(entity);
					if (unit is EnemyUnit)
						enemyCastSystem.CancelCast(entity);
					continue;
				}
				cast.Elapsed += deltaTime;
				if (!cast.Fired)
				{
					if (cast.Telegraph != null)
						cast.Telegraph.SetProgress(def.CastPoint > 0f ? cast.Elapsed / def.CastPoint : 1f);
					if (cast.Elapsed < def.CastPoint)
						continue;
					cast.Fired = true;
					if (cast.Telegraph != null)
					{
						cast.Telegraph.Despawn();
						cast.Telegraph = null;
					}
					SkillSlot slot = cast.Slot;
					UnitCore target = SkillTargetQuery.IsAlive(cast.Target) ? cast.Target : null;
					bool anchored = def.TargetType == SkillTargetType.Point || def.Telegraph != null && def.Telegraph.AnchorOnTarget;
					Vector3 point = anchored ? cast.Point : GetPoint(unit, def, target);
					float total = def.CastPoint + def.Backswing;
					Vector3 direction = cast.Direction;
					Fire(unit, slot, target, point, direction);
					// Effects may add components elsewhere; re-read before touching this entity again.
					if (castingStash.Has(entity) && castingStash.Get(entity).Elapsed >= total)
						castingStash.Remove(entity);
					continue;
				}
				if (cast.Elapsed >= def.CastPoint + def.Backswing)
					castingStash.Remove(entity);
			}
		}

		private void Fire(UnitCore unit, SkillSlot slot, UnitCore target, Vector3 point, Vector3 direction)
		{
			SkillCastContext context = skillService.CreateContext(unit, slot.Def, slot.Level, 1f, target, point, null);
			context.Direction = direction;
			SkillService.Execute(slot.Def.Effects, context);
		}
	}
}
