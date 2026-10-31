using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Skills.Components;
using Codebase.Skills.Effects;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Skills
{
	// Owned by SkillService; the registered auto-cast system supplies its clock.
	public class SkillForcedMovement
	{
		public enum Kind { Knockback, Pull, Dash }

		public sealed class State
		{
			public UnitCore Unit;
			public Entity Entity;
			public Kind Kind;
			public SkillCastContext Context;
			public Vector3 Direction;
			public float Remaining;
			public float Speed;
			public DashEffect Dash;
			public bool Rotation;
			public readonly HashSet<UnitCore> Hit = new HashSet<UnitCore>();
		}

		private struct Contact
		{
			public UnitCore Unit;
			public float Distance;
		}

		private readonly List<State> active = new List<State>();
		private readonly List<State> frame = new List<State>();
		private World world;
		private Stash<SkillForcedMovementComponent> stash;

		private bool Bind()
		{
			if (world == World.Default) return world != null;
			active.Clear();
			world = World.Default;
			if (world == null) return false;
			stash = world.GetStash<SkillForcedMovementComponent>();
			return true;
		}

		public bool Has(UnitCore unit, bool dashOnly = false)
		{
			if (!Bind()) return false;
			if (unit == null || unit.EntityProvider == null) return false;
			Entity entity = unit.EntityProvider.Entity;
			return !world.IsDisposed(entity) && stash.Has(entity) && (!dashOnly || stash.Get(entity).Movement.Kind == Kind.Dash);
		}

		public void Stop(Entity entity, bool dashOnly = false)
		{
			if (!Bind()) return;
			if (world.IsDisposed(entity) || !stash.Has(entity)) return;
			State state = stash.Get(entity).Movement;
			if (dashOnly && state.Kind != Kind.Dash) return;
			End(state);
		}

		private bool Current(State state)
		{
			return !world.IsDisposed(state.Entity) && stash.Has(state.Entity) && ReferenceEquals(stash.Get(state.Entity).Movement, state);
		}

		private void End(State state)
		{
			active.Remove(state);
			if (!Current(state)) return;
			stash.Remove(state.Entity);
			UnitCore unit = state.Unit;
			if (unit == null) return;
			if (unit is EnemyUnit enemy)
			{
				// ResetPath invalidated the AI's cached destination, so force a fresh path.
				enemy.LastDestination = null;
				enemy.LastWanderDestination = null;
			}
			NavMeshAgent agent = unit.Agent;
			if (agent != null && agent.enabled && agent.isOnNavMesh)
			{
				agent.updateRotation = state.Rotation;
				if (unit is EnemyUnit || unit is PetUnit || global::Codebase.Rpg.RpgControl.IsHero(unit))
					agent.isStopped = false;
			}
		}

		public void Start(in SkillCastContext context, UnitCore unit, Kind kind, Vector3 direction, float distance, float speed, DashEffect dash = null)
		{
			if (!Bind()) return;
			if (!SkillTargetQuery.IsAlive(unit) || unit.EntityProvider == null) return;
			Entity entity = unit.EntityProvider.Entity;
			if (world.IsDisposed(entity) || world.GetStash<UnitDeadMarker>().Has(entity)) return;
			Stop(entity);
			if (distance <= 0f || speed <= 0f) return;
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.0001f) direction = SkillTargetQuery.GetFlatDirection(unit, null);
			var state = new State { Unit = unit, Entity = entity, Kind = kind, Context = context, Direction = direction.normalized, Remaining = distance, Speed = speed, Dash = dash };
			NavMeshAgent agent = unit.Agent;
			state.Rotation = agent != null && agent.updateRotation;
			if (agent != null && agent.enabled && agent.isOnNavMesh)
			{
				agent.updateRotation = false;
				if (unit is EnemyUnit || unit is PetUnit || global::Codebase.Rpg.RpgControl.IsAiHero(unit))
				{
					agent.isStopped = true;
					agent.ResetPath();
				}
			}
			if (kind == Kind.Dash) unit.transform.rotation = Quaternion.LookRotation(state.Direction);
			stash.Set(entity, new SkillForcedMovementComponent { Movement = state });
			active.Add(state);
		}

		public void Tick(SkillService service, float deltaTime)
		{
			if (!Bind()) return;
			// OnHit can add or replace states; new displacements start moving next frame.
			frame.Clear();
			frame.AddRange(active);
			foreach (State state in frame)
			{
				UnitCore unit = state.Unit;
				if (!Current(state) || !SkillTargetQuery.IsAlive(unit) || unit.EntityProvider == null || !unit.EntityProvider.Entity.Equals(state.Entity) ||
					world.GetStash<UnitDeadMarker>().Has(state.Entity) || unit is PlayerUnit hero && hero.IsDying ||
					state.Kind == Kind.Dash && service.IsStunned(unit))
				{
					End(state);
					continue;
				}
				if (state.Kind == Kind.Pull && state.Context.Caster != null)
				{
					Vector3 toward = state.Context.Caster.transform.position - unit.transform.position;
					toward.y = 0f;
					float gap = Mathf.Max(0f, toward.magnitude - SkillTargetQuery.GetUnitRadius(unit) - SkillTargetQuery.GetUnitRadius(state.Context.Caster));
					state.Remaining = Mathf.Min(state.Remaining, gap);
					if (toward.sqrMagnitude > 0.0001f) state.Direction = toward.normalized;
				}
				if (state.Kind == Kind.Dash) unit.transform.rotation = Quaternion.LookRotation(state.Direction);
				float step = Mathf.Min(state.Remaining, state.Speed * Mathf.Max(0f, deltaTime));
				Vector3 from = unit.transform.position;
				Vector3 to = from + state.Direction * step;
				bool ended = step >= state.Remaining;
				if (!Clip(unit, from, ref to)) ended = true;
				if (state.Kind == Kind.Dash)
					ended |= DashStep(service, state, from, ref to);
				if (!Current(state)) continue;
				Move(unit, to);
				state.Remaining -= Mathf.Sqrt(SkillTargetQuery.FlatSqrDistance(from, to));
				if (ended || state.Remaining <= 0.0001f) End(state);
			}
			frame.Clear();
		}

		private bool DashStep(SkillService service, State state, Vector3 from, ref Vector3 to)
		{
			UnitCore caster = state.Unit;
			DashEffect def = state.Dash;
			float length = Mathf.Sqrt(SkillTargetQuery.FlatSqrDistance(from, to));
			float end = length;
			bool ended = false;
			float halfWidth = Mathf.Max(0f, def.Width) * 0.5f;
			// Line telegraph: hits never reach past the drawn half-width (same rule as SkillLinearMissiles).
			TelegraphDef telegraph = state.Context.Skill != null ? state.Context.Skill.Telegraph : null;
			float maxHitRadius = telegraph != null && telegraph.Shape == TelegraphShape.Line
				? Mathf.Max(halfWidth, (telegraph.Width > 0f ? telegraph.Width : 1f) * 0.5f)
				: float.PositiveInfinity;
			List<UnitCore> candidates = SkillTargetQuery.RentList();
			var contacts = new List<Contact>();
			try
			{
				candidates.AddRange(SkillTargetQuery.GetCandidates(caster));
				// Static and far bodies, which ordinary target searches omit, can still block the dash (never hit, see below).
				if (GameContext.Has<EnemiesModel>())
					foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
						if (enemy != null && !candidates.Contains(enemy)) candidates.Add(enemy);
				foreach (UnitCore unit in candidates)
				{
					if (unit == caster || !SkillTargetQuery.IsAlive(unit)) continue;
					float radius = SkillTargetQuery.GetUnitRadius(unit);
					float body = def.PassThrough ? float.PositiveInfinity : ContactDistance(from, state.Direction, length, unit.transform.position, SkillTargetQuery.GetUnitRadius(caster) + radius);
					if (body <= end) { end = body; ended = true; }
					// Static and far bodies only block the dash; like every other skill search, it never hits them.
					if (unit is EnemyUnit other && (other.IsStatic || other.IsFar)) continue;
					if (state.Hit.Contains(unit) || !SkillTargetQuery.Matches(caster, unit, def.Filter)) continue;
					float hitRadius = Mathf.Min(halfWidth + radius, maxHitRadius);
					float hit = ContactDistance(from, state.Direction, length, unit.transform.position, hitRadius);
					// A matching body inside the hit lane receives its hit before it blocks movement;
					// a body outside the lane only stops the dash.
					if (body < hit && SideSqr(from, state.Direction, unit.transform.position) <= hitRadius * hitRadius)
						hit = body;
					if (hit <= length)
					{
						contacts.Add(new Contact { Unit = unit, Distance = hit });
						if (def.StopAtFirstHit && hit <= end) { end = hit; ended = true; }
					}
				}
				contacts.Sort((a, b) => a.Distance.CompareTo(b.Distance));
				foreach (Contact contact in contacts)
				{
					if (contact.Distance > end + 0.0001f || !Current(state)) break;
					if (!SkillTargetQuery.IsAlive(contact.Unit)) continue;
					Move(caster, from + state.Direction * contact.Distance);
					state.Hit.Add(contact.Unit);
					SkillCastContext impact = state.Context;
					impact.PrimaryTarget = contact.Unit;
					impact.Point = contact.Unit.transform.position;
					impact.Direction = state.Direction;
					if (def.Power > 0f) service.DealDamage(impact, contact.Unit, def.Power * impact.Power, null, false);
					SkillService.Execute(def.OnHit, impact);
					if (!SkillTargetQuery.IsAlive(caster) || service.IsStunned(caster))
					{
						End(state);
						break;
					}
					if (def.StopAtFirstHit) break;
				}
				to = Vector3.Lerp(from, to, length > 0f ? end / length : 0f);
				return ended;
			}
			finally
			{
				SkillTargetQuery.ReturnList(candidates);
			}
		}

		private static float SideSqr(Vector3 from, Vector3 direction, Vector3 center)
		{
			Vector3 offset = center - from;
			offset.y = 0f;
			float along = Vector3.Dot(offset, direction);
			return Mathf.Max(0f, offset.sqrMagnitude - along * along);
		}

		private static float ContactDistance(Vector3 from, Vector3 direction, float length, Vector3 center, float radius)
		{
			Vector3 offset = center - from;
			offset.y = 0f;
			float along = Vector3.Dot(offset, direction);
			float sideSqr = Mathf.Max(0f, offset.sqrMagnitude - along * along);
			if (sideSqr > radius * radius) return float.PositiveInfinity;
			float chord = Mathf.Sqrt(Mathf.Max(0f, radius * radius - sideSqr));
			float enter = Mathf.Max(0f, along - chord);
			return along + chord >= 0f && enter <= length ? enter : float.PositiveInfinity;
		}

		private static bool Clip(UnitCore unit, Vector3 from, ref Vector3 to)
		{
			NavMeshAgent agent = unit.Agent;
			int mask = agent != null ? agent.areaMask : NavMesh.AllAreas;
			bool flying = unit is EnemyUnit enemy && enemy.IsFlying;
			Vector3 origin = from;
			if (flying || agent == null || !agent.enabled)
			{
				if (!NavMesh.SamplePosition(from, out NavMeshHit ground, 3f, mask)) { to = from; return false; }
				origin = ground.position;
			}
			else if (!agent.isOnNavMesh) { to = from; return false; }
			Vector3 desired = new Vector3(to.x, origin.y, to.z);
			if (NavMesh.Raycast(origin, desired, out NavMeshHit edge, mask))
			{
				to = new Vector3(edge.position.x, flying ? from.y : edge.position.y, edge.position.z);
				return false;
			}
			if (!NavMesh.SamplePosition(desired, out NavMeshHit sample, 1.5f, mask) || SkillTargetQuery.FlatSqrDistance(desired, sample.position) > 0.01f) { to = from; return false; }
			to = new Vector3(desired.x, flying ? from.y : sample.position.y, desired.z);
			return true;
		}

		private static void Move(UnitCore unit, Vector3 to)
		{
			NavMeshAgent agent = unit.Agent;
			if (agent != null && agent.enabled && agent.isOnNavMesh)
				agent.Move(to - unit.transform.position);
			else
				unit.transform.position = to;
		}

		public void Blink(in SkillCastContext context, BlinkEffect def)
		{
			UnitCore caster = context.Caster;
			if (!SkillTargetQuery.IsAlive(caster)) return;
			Vector3 from = caster.transform.position;
			UnitCore target = SkillTargetQuery.IsAlive(context.PrimaryTarget) ? context.PrimaryTarget : null;
			Vector3 direction = SkillTargetQuery.GetFlatDirection(caster, target);
			float distance = Mathf.Max(0f, def.Distance);
			Vector3 to;
			switch (def.Mode)
			{
				case BlinkMode.ToPoint:
					Vector3 offset = context.Point - from;
					offset.y = 0f;
					to = distance > 0f && offset.magnitude > distance ? from + offset.normalized * distance : context.Point;
					break;
				case BlinkMode.AwayFromTarget:
					to = from - direction * distance;
					break;
				default:
					to = target != null ? target.transform.position + direction * (distance + SkillTargetQuery.GetUnitRadius(caster) + SkillTargetQuery.GetUnitRadius(target)) : from + direction * distance;
					break;
			}
			NavMeshAgent agent = caster.Agent;
			int mask = agent != null ? agent.areaMask : NavMesh.AllAreas;
			if (!NavMesh.SamplePosition(to, out NavMeshHit sample, 1.5f, mask)) return;
			to = sample.position;
			if (def.Mode == BlinkMode.ToPoint && distance > 0f)
			{
				// Sampling near an edge must not extend a range-limited blink.
				float sampledDistance = Mathf.Sqrt(SkillTargetQuery.FlatSqrDistance(from, to));
				if (sampledDistance > distance) to = Vector3.Lerp(from, to, distance / sampledDistance);
			}
			if (!NavMesh.SamplePosition(from, out NavMeshHit origin, 3f, mask)) return;
			if (NavMesh.Raycast(origin.position, to, out NavMeshHit edge, mask)) to = edge.position;
			if (caster.EntityProvider != null) Stop(caster.EntityProvider.Entity);
			if (caster is EnemyUnit flying && flying.IsFlying) to.y = from.y;
			if (agent != null && agent.enabled)
			{
				if (!agent.Warp(to)) return;
				if (agent.isOnNavMesh) agent.ResetPath();
			}
			else caster.transform.position = to;
			if (caster is EnemyUnit enemy) { enemy.LastDestination = null; enemy.LastWanderDestination = null; }
			if (def.Mode == BlinkMode.BehindTarget && target != null)
				caster.transform.rotation = Quaternion.LookRotation(SkillTargetQuery.GetFlatDirection(caster, target));
		}
	}
}
