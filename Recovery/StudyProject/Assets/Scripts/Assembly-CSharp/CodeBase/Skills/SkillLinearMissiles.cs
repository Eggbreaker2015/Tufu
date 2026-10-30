using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Skills.Effects;
using Codebase.Unit;
using Codebase.Utilities.ObjectPool;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Skills
{
	public class SkillLinearMissiles
	{
		private class Missile
		{
			public SkillCastContext Context;

			public LinearMissileEffect Def;

			public Vector3 Position;

			public Vector3 Direction;

			public float Speed;

			public float Travelled;

			public float Range;

			public float Radius;

			// Line telegraph half-width: Radius + unit radius never reaches past the drawn strip (0 = no cap).
			public float MaxHitRadius;

			public bool Pierce;

			public SkillTargetFilter Filter;

			public float Power;

			public WeaponType? DamageType;

			public ParticleSystem Visual;

			public bool WallCheck;

			public Vector3 WallFrom;

			// RPG: flying over a ledge gap; wall checks resume once Travelled reaches this.
			public float WallSkipUntil;

			// The caster has been seen dead or pooled; if it comes back alive it is a new life and the missile ends.
			public bool CasterDied;

			public readonly List<UnitCore> Hit = new List<UnitCore>();
		}

		// Ray origin re-snap after each clear step.
		private const float WallResampleDistance = 1.5f;

		// RPG ledges: how far past a NavMesh edge to look for ground to fly on to, and how close a sample must be to the probe.
		private const float LedgeGapDistance = 2f;

		private const float LedgeProbeStep = 0.2f;

		private readonly List<Missile> missiles = new List<Missile>();

		private readonly Stack<Missile> pool = new Stack<Missile>();

		private readonly List<float> hitTimes = new List<float>(16);

		public void Launch(SkillService service, in SkillCastContext context, LinearMissileEffect def)
		{
			Vector3 baseDirection = context.Direction.sqrMagnitude > 0.0001f ? context.Direction : SkillTargetQuery.GetFlatDirection(context.Caster, context.PrimaryTarget);
			baseDirection.y = 0f;
			baseDirection = baseDirection.sqrMagnitude > 0.0001f ? baseDirection.normalized : SkillTargetQuery.GetFlatDirection(context.Caster, context.PrimaryTarget);
			Vector3 origin = context.Caster.transform.position;
			bool wallCheck = NavMesh.SamplePosition(origin, out NavMeshHit ground, 1f, NavMesh.AllAreas);
			SkillVfxEntry vfx = string.IsNullOrEmpty(def.VfxKey) ? null : SkillLibrary.FindVfx(def.VfxKey);
			ParticleSystem prefab = vfx == null ? null : vfx.Prefab != null ? vfx.Prefab : SkillProjectileView.GetTemplate(vfx);
			TelegraphDef telegraph = context.Skill != null ? context.Skill.Telegraph : null;
			float maxHitRadius = telegraph != null && telegraph.Shape == TelegraphShape.Line ? (telegraph.Width > 0f ? telegraph.Width : 1f) * 0.5f : 0f;
			int count = Mathf.Max(1, def.Count);
			for (int i = 0; i < count; i++)
			{
				float offset = count > 1 ? Mathf.Lerp(-def.SpreadAngle * 0.5f, def.SpreadAngle * 0.5f, (float)i / (count - 1)) : 0f;
				Vector3 direction = Quaternion.AngleAxis(offset, Vector3.up) * baseDirection;
				Missile missile = pool.Count > 0 ? pool.Pop() : new Missile();
				missile.Context = context;
				missile.Def = def;
				missile.Position = origin + direction * 0.5f;
				missile.Direction = direction;
				missile.Speed = Mathf.Max(0.5f, def.Speed);
				missile.Travelled = 0f;
				missile.Range = Mathf.Max(0.5f, def.Range);
				missile.Radius = Mathf.Max(0.05f, def.Radius);
				missile.MaxHitRadius = maxHitRadius;
				missile.Pierce = def.Pierce;
				missile.Filter = def.Filter;
				missile.Power = def.Power * context.Power;
				missile.DamageType = def.OverrideDamageType ? def.DamageType : (WeaponType?)null;
				missile.WallCheck = wallCheck;
				missile.WallFrom = ground.position;
				missile.WallSkipUntil = 0f;
				missile.CasterDied = false;
				missile.Hit.Clear();
				if (prefab != null)
				{
					missile.Visual = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, missile.Position + Vector3.up * SkillService.MissileHeight, Quaternion.LookRotation(direction));
					missile.Visual.Clear(true);
					missile.Visual.Play(true);
					if (missile.Visual.TryGetComponent(out SkillProjectileView view))
						view.Aim(direction);
				}
				missiles.Add(missile);
			}
		}

		public void Tick(SkillService service, float deltaTime)
		{
			for (int i = missiles.Count - 1; i >= 0; i--)
			{
				Missile missile = missiles[i];
				UnitCore caster = missile.Context.Caster;
				if (caster == null)
				{
					Recycle(i);
					continue;
				}
				// A dead caster's missile flies on and still hits; a pooled caster alive again is a new spawn.
				if (!SkillTargetQuery.IsAlive(caster))
					missile.CasterDied = true;
				else if (missile.CasterDied)
				{
					Recycle(i);
					continue;
				}
				float remaining = Mathf.Max(0f, missile.Range - missile.Travelled);
				float step = Mathf.Min(missile.Speed * Mathf.Max(0f, deltaTime), remaining);
				Vector3 a = missile.Position;
				Vector3 b = a + missile.Direction * step;
				// End on the final step even when world-position rounding loses a tiny remaining distance.
				bool ended = step >= remaining;
				if (missile.WallCheck && missile.Travelled >= missile.WallSkipUntil)
				{
					if (NavMesh.Raycast(missile.WallFrom, new Vector3(b.x, missile.WallFrom.y, b.z), out NavMeshHit wall, NavMesh.AllAreas))
					{
						// RPG: a low ledge (rock, stump, step) is a NavMesh edge too, but the missile flies over it at MissileHeight.
						if (global::Codebase.Rpg.RpgModeConfig.Enabled && TryFindLanding(missile.WallFrom, wall.position, missile.Direction, out NavMeshHit landing))
						{
							b.y += landing.position.y - missile.WallFrom.y;
							missile.WallFrom = landing.position;
							missile.WallSkipUntil = missile.Travelled + Vector2.Distance(new Vector2(a.x, a.z), new Vector2(landing.position.x, landing.position.z));
						}
						else
						{
							b = new Vector3(wall.position.x, a.y, wall.position.z);
							ended = true;
						}
					}
					// Keep the next ray origin on the surface the missile is over (slopes, stairs, bridges).
					else if (NavMesh.SamplePosition(new Vector3(b.x, missile.WallFrom.y, b.z), out NavMeshHit ground, WallResampleDistance, NavMesh.AllAreas))
					{
						b.y += ground.position.y - missile.WallFrom.y;
						missile.WallFrom = ground.position;
					}
					else
						ended = true;
				}
				List<UnitCore> hits = SkillTargetQuery.RentList();
				hitTimes.Clear();
				try
				{
					// RPG: the missile also hits trees and other static enemies.
					if (global::Codebase.Rpg.RpgModeConfig.Enabled) SkillTargetQuery.RpgHitStatic = true;
					List<UnitCore> candidates = SkillTargetQuery.GetCandidates(caster);
					if (global::Codebase.Rpg.RpgModeConfig.Enabled) SkillTargetQuery.RpgHitStatic = false;
					for (int j = 0; j < candidates.Count; j++)
					{
						UnitCore unit = candidates[j];
						if (missile.Hit.Contains(unit) || !SkillTargetQuery.Matches(caster, unit, missile.Filter))
							continue;
						float radius = missile.Radius + SkillTargetQuery.GetUnitRadius(unit);
						if (missile.MaxHitRadius > 0f)
							radius = Mathf.Min(radius, Mathf.Max(missile.Radius, missile.MaxHitRadius));
						if (SkillTargetQuery.FlatSqrDistanceToSegment(unit.transform.position, a, b, out float t) > radius * radius)
							continue;
						hits.Add(unit);
						hitTimes.Add(t);
					}
					// Resolve only after collecting: nested effects reuse the shared candidate list.
					if (!missile.Pierce && hits.Count > 0)
					{
						int nearest = 0;
						for (int j = 1; j < hits.Count; j++)
							if (hitTimes[j] < hitTimes[nearest])
								nearest = j;
						b = Vector3.Lerp(a, b, hitTimes[nearest]);
						ended = true;
						Resolve(service, missile, hits[nearest]);
					}
					else if (missile.Pierce)
						for (int j = 0; j < hits.Count; j++)
						{
							missile.Hit.Add(hits[j]);
							Resolve(service, missile, hits[j]);
						}
				}
				finally
				{
					SkillTargetQuery.ReturnList(hits);
					hitTimes.Clear();
				}
				missile.Position = b;
				missile.Travelled += Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
				if (missile.Visual != null)
					missile.Visual.transform.position = b + Vector3.up * SkillService.MissileHeight;
				if (ended || missile.Travelled >= missile.Range)
					Recycle(i);
			}
		}

		// A dead caster still deals damage from its last stats (spec A2); lifesteal skips dead sources, thorns only reach the corpse.
		private static void Resolve(SkillService service, Missile missile, UnitCore unit)
		{
			if (missile.Context.Caster == null || unit == null)
				return;
			service.DealDamage(missile.Context, unit, missile.Power, missile.DamageType, false);
			SkillCastContext impact = missile.Context;
			impact.PrimaryTarget = unit;
			impact.Point = unit.transform.position;
			SkillService.Execute(missile.Def.OnImpact, impact);
		}

		// Walkable surface within LedgeGapDistance past the edge and within missile height of the current one
		// = a rock, stump or step the missile flies over (the baked NavMesh leaves a gap around it). Else a wall.
		private static bool TryFindLanding(Vector3 from, Vector3 edge, Vector3 direction, out NavMeshHit landing)
		{
			for (float distance = LedgeProbeStep; distance <= LedgeGapDistance + 0.001f; distance += LedgeProbeStep)
			{
				Vector3 probe = new Vector3(edge.x, from.y, edge.z) + direction * distance;
				if (!NavMesh.SamplePosition(probe, out landing, SkillService.MissileHeight, NavMesh.AllAreas))
					continue;
				Vector3 offset = landing.position - probe;
				if (Mathf.Abs(offset.y) < SkillService.MissileHeight && offset.x * offset.x + offset.z * offset.z <= LedgeProbeStep * LedgeProbeStep)
					return true;
			}
			landing = default;
			return false;
		}

		private void Recycle(int index)
		{
			Missile missile = missiles[index];
			missiles.RemoveAt(index);
			if (missile.Visual != null)
				ObjectPoolUtility<ParticleSystem>.Despawn(missile.Visual);
			missile.Visual = null;
			missile.Context = default;
			missile.Def = null;
			missile.Hit.Clear();
			pool.Push(missile);
		}

		public void Clear()
		{
			for (int i = missiles.Count - 1; i >= 0; i--)
				Recycle(i);
		}
	}
}
