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
	// Runs before EnemyCastSystem: pending missiles, cast progress (cast point -> effects -> channel -> backswing), cooldowns,
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

		[Inject]
		private Stash<UnitDeadMarker> deadStash;

		private PlayerUnit installedHero;

		private bool heroWasDown;

		private Filter casterFilter;

		private Filter castingFilter;

		public override void OnAwake()
		{
			casterFilter = Filter.With<SkillCasterComponent>().Build();
			castingFilter = Filter.With<SkillCastingComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			PlayerUnit hero = playerModel.Unit;
			if (hero != null && hero.EntityProvider != null && !World.IsDisposed(hero.EntityProvider.Entity))
			{
				if (hero != installedHero)
				{
					UnitSkillLoadout loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Hero, "hero");
					Entity entity = hero.EntityProvider.Entity;
					if (loadout != null)
						skillService.Install(hero, entity, loadout);
					else if (casterStash.Has(entity))
						casterStash.Remove(entity);
					installedHero = hero;
					heroWasDown = false;
				}
				bool down = hero.IsDying || hero.IsDead;
				if (down)
					skillService.CancelCast(hero);
				else if (heroWasDown)
					skillService.ResetCaster(hero);
				heroWasDown = down;
			}
			skillService.TickForcedMovement(deltaTime);
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
				if (unit is PetUnit && (!PetConfig.Enabled || deadStash.Has(entity)))
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

		// Enemies only act on skills in combat, never while evading; anything else always.
		private bool IsEngaged(UnitCore unit)
		{
			if (!(unit is EnemyUnit enemy))
				return true;
			return !enemy.IsReturningHome && (enemy.AggroTimer > 0f || playerModel.Unit != null && playerModel.Unit.CurrentTarget == enemy);
		}

		private bool CanThink(Entity entity, UnitCore unit)
		{
			if (stunStash.Has(entity) || castingStash.Has(entity) || skillService.IsDisplaced(unit) || skillService.IsSilenced(entity))
				return false;
			if (unit is EnemyUnit enemy)
			{
				if (enemy.IsCasting || enemyCastSystem.IsCasting(entity) || spawnActiveStash.Has(entity) || waitingStash.Has(entity) || enemy.RetreatTimer > 0f)
					return false;
				PlayerUnit player = playerModel.Unit;
				if (player == null || player.IsDying || player.IsSleeping || !playerModel.CanMove)
					return false;
			}
			if (unit is PetUnit)
				return !deadStash.Has(entity) && CanFriendlyAct(playerModel.Unit);
			if (unit is PlayerUnit hero)
				return CanFriendlyAct(hero);
			return true;
		}

		private bool CanFriendlyAct(PlayerUnit hero)
		{
			return hero != null && playerModel.CanMove && !hero.IsDying && !hero.IsDead && !hero.IsSleeping;
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
					if (!(unit is EnemyUnit))
						return SkillTargetQuery.IsAlive(hero) && !hero.IsDying && SkillTargetQuery.FlatDistance(unit, hero) <= range ? hero : null;
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
			if (target != null && target != unit && !global::Codebase.Rpg.RpgControl.IsControlled(unit))
			{
				if (unit is EnemyUnit turningEnemy && turningEnemy.Agent != null && turningEnemy.Agent.enabled && turningEnemy.Agent.isOnNavMesh)
					turningEnemy.Agent.updateRotation = false;
				unit.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
			}
			Vector3 point = GetPoint(unit, def, target);
			float duration = Mathf.Max(0f, def.CastPoint) + Mathf.Max(0f, def.ChannelDuration) + Mathf.Max(0f, def.Backswing);
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
			if ((def.CastPoint > 0f || def.ChannelDuration > 0f) && unit is EnemyUnit && def.Telegraph != null)
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
				cast.Channeling = def.ChannelDuration > 0f;
				if (cast.Telegraph != null)
				{
					cast.Telegraph.SetProgress(1f);
					cast.Telegraph.LockPosition();
				}
				Fire(unit, slot, target, point, direction);
			}
		}

		private bool Interrupted(Entity entity, in SkillCastingComponent cast)
		{
			UnitCore unit = cast.Caster;
			if (unit == null || unit.IsDead || !unit.gameObject.activeInHierarchy || unit.EntityProvider == null ||
				!unit.EntityProvider.Entity.Equals(entity) || cast.Slot == null || deadStash.Has(entity)) return true;
			if (unit is PlayerUnit dying && dying.IsDying || unit is PetUnit && !PetConfig.Enabled) return true;
			if (cast.Fired && !cast.Channeling) return false;
			return stunStash.Has(entity) || skillService.IsSilenced(entity) ||
				unit is PetUnit && !CanFriendlyAct(playerModel.Unit) || unit is PlayerUnit hero && !CanFriendlyAct(hero);
		}

		private void UpdateCasts(float deltaTime)
		{
			foreach (Entity entity in castingFilter)
			{
				// Effects can resize stashes or cancel this cast; never retain a ref across resolution.
				SkillCastingComponent cast = castingStash.Get(entity);
				if (Interrupted(entity, cast))
				{
					skillService.CancelCast(entity);
					continue;
				}
				SkillDef def = cast.Slot.Def;
				float castPoint = Mathf.Max(0f, def.CastPoint);
				float channelDuration = Mathf.Max(0f, def.ChannelDuration);
				cast.Elapsed += deltaTime;
				castingStash.Set(entity, cast);
				if (!cast.Fired)
				{
					if (cast.Telegraph != null) cast.Telegraph.SetProgress(castPoint > 0f ? cast.Elapsed / castPoint : 1f);
					if (cast.Elapsed < castPoint) continue;
					cast.Fired = true;
					cast.Channeling = channelDuration > 0f;
					if (!cast.Channeling) DespawnTelegraph(ref cast);
					else if (cast.Telegraph != null)
					{
						cast.Telegraph.SetProgress(1f);
						cast.Telegraph.LockPosition();
					}
					castingStash.Set(entity, cast);
					UnitCore target = SkillTargetQuery.IsAlive(cast.Target) ? cast.Target : null;
					bool anchored = def.TargetType == SkillTargetType.Point || def.Telegraph != null && def.Telegraph.AnchorOnTarget;
					Vector3 point = anchored ? cast.Point : GetPoint(cast.Caster, def, target);
					Fire(cast.Caster, cast.Slot, target, point, cast.Direction);
					if (World.IsDisposed(entity) || !castingStash.Has(entity)) continue;
					cast = castingStash.Get(entity);
				}
				if (cast.Channeling)
				{
					cast.ChannelElapsed = Mathf.Clamp(cast.Elapsed - castPoint, 0f, channelDuration);
					if (cast.Telegraph != null) cast.Telegraph.SetProgress(1f);
					float interval = def.ChannelTickInterval > 0f ? Mathf.Max(0.05f, def.ChannelTickInterval) : channelDuration;
					while ((def.ChannelTickInterval > 0f || cast.ChannelTicks == 0) && (cast.ChannelTicks + 1) * interval <= channelDuration + 0.0001f &&
						(cast.ChannelTicks + 1) * interval <= cast.ChannelElapsed + 0.0001f)
					{
						if (Interrupted(entity, cast)) break;
						cast.ChannelTicks++;
						UnitCore target = SkillTargetQuery.IsAlive(cast.Target) ? cast.Target : null;
						if (!(cast.Caster is EnemyUnit) && cast.Target != cast.Caster)
							target = ReaimChannel(ref cast, def, target);
						castingStash.Set(entity, cast);
						SkillCastContext context = skillService.CreateContext(cast.Caster, def, cast.Slot.Level, 1f, target, cast.Point, null);
						context.Direction = cast.Direction;
						SkillService.Execute(def.OnChannelTick, context);
						if (World.IsDisposed(entity) || !castingStash.Has(entity)) break;
						cast = castingStash.Get(entity);
						if (def.ChannelTickInterval <= 0f) break;
					}
					if (World.IsDisposed(entity) || !castingStash.Has(entity)) continue;
					if (Interrupted(entity, cast))
					{
						skillService.CancelCast(entity);
						continue;
					}
					if (cast.ChannelElapsed >= channelDuration)
					{
						cast.Channeling = false;
						DespawnTelegraph(ref cast);
					}
					castingStash.Set(entity, cast);
				}
				float total = castPoint + channelDuration + Mathf.Max(0f, def.Backswing);
				if (cast.Elapsed >= total && !skillService.IsDashing(cast.Caster)) castingStash.Remove(entity);
			}
		}

		// Friendly channels draw no telegraph, so each tick re-aims at the live target, or at the next one
		// in range when it died. Enemy channels keep the direction locked to match their telegraph.
		private UnitCore ReaimChannel(ref SkillCastingComponent cast, SkillDef def, UnitCore target)
		{
			if (target == null)
			{
				UnitCore next = Pick(cast.Caster, def);
				if (next != null && next != cast.Caster && (def.Range <= 0f || SkillTargetQuery.FlatDistance(cast.Caster, next) <= def.Range))
					target = next;
			}
			if (target == null || target == cast.Caster)
				return target;
			Vector3 aim = SkillTargetQuery.GetFlatDirection(cast.Caster, target);
			if (aim.sqrMagnitude <= 0.0001f)
				return target;
			cast.Target = target;
			cast.Direction = aim;
			cast.Point = GetPoint(cast.Caster, def, target);
			if (!global::Codebase.Rpg.RpgControl.IsControlled(cast.Caster))
				cast.Caster.transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
			return target;
		}

		private static void DespawnTelegraph(ref SkillCastingComponent cast)
		{
			if (cast.Telegraph == null) return;
			cast.Telegraph.Despawn();
			cast.Telegraph = null;
		}

		private void Fire(UnitCore unit, SkillSlot slot, UnitCore target, Vector3 point, Vector3 direction)
		{
			SkillCastContext context = skillService.CreateContext(unit, slot.Def, slot.Level, 1f, target, point, null);
			context.Direction = direction;
			SkillService.Execute(slot.Def.Effects, context);
		}
	}
}
