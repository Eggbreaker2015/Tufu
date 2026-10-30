using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Skills;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Utilities.ObjectPool;
using Codebase.Vfx;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Abilities.Vfx.Systems
{
	// Skills: circling stars over stunned heroes and pets (SkillLibrary vfx "stun_stars"); enemies keep their frozen stun pose.
	public class StunVfxSystem : SystemBase
	{
		private const string VfxKey = "stun_stars";

		private static readonly bool ShowOnEnemies = false;

		private const float SpinSpeed = 180f;

		private const float HeadOffset = 0.5f;

		private Filter filter;

		private SkillVfxEntry entry;

		private bool vfxResolved;

		[Inject]
		private Stash<BuffStunComponent> stunStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		private readonly Dictionary<Entity, ParticleSystem> activeVfx = new Dictionary<Entity, ParticleSystem>();

		private readonly List<Entity> toRemove = new List<Entity>();

		public override void OnAwake()
		{
			filter = Filter.With<UnitComponent>().With<BuffStunComponent>().Build();
		}

		public override void Dispose()
		{
			foreach (var pair in activeVfx)
				Despawn(pair.Value);
			activeVfx.Clear();
			toRemove.Clear();
		}

		public override void OnUpdate(float deltaTime)
		{
			toRemove.Clear();
			foreach (var pair in activeVfx)
				if (World.IsDisposed(pair.Key) || !stunStash.Has(pair.Key) || !unitStash.Has(pair.Key) || !CanShow(unitStash.Get(pair.Key).Core))
					toRemove.Add(pair.Key);
			foreach (Entity entity in toRemove)
			{
				Despawn(activeVfx[entity]);
				activeVfx.Remove(entity);
			}

			foreach (Entity entity in filter)
			{
				UnitCore core = unitStash.Get(entity).Core;
				if (!CanShow(core))
					continue;
				if (!vfxResolved)
				{
					entry = SkillLibrary.FindVfx(VfxKey);
					vfxResolved = true;
				}
				if (entry == null || entry.Prefab == null)
					return;
				ParticleSystem prefab = entry.Prefab;
				Vector3 position = GetHeadPosition(core);
				Quaternion rotation = Quaternion.AngleAxis(Time.time * SpinSpeed, Vector3.up) * prefab.transform.rotation;
				if (!activeVfx.TryGetValue(entity, out ParticleSystem visual) || visual == null)
				{
					visual = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, position, rotation);
					if (visual == null)
						continue;
					visual.Clear(true);
					visual.Play(true);
					activeVfx[entity] = visual;
				}
				visual.transform.SetPositionAndRotation(position, rotation);
				// Sized by the stunned character, following later size changes.
				visual.transform.localScale = prefab.transform.localScale * ((entry.Scale > 0f ? entry.Scale : 1f) * CharacterVfxScale.Factor(core.transform));
			}
		}

		private static bool CanShow(UnitCore core)
		{
			return core != null && !core.IsDead && !core.IsDying && !core.IsReviving && (core is PlayerUnit || core is PetUnit || ShowOnEnemies && core is EnemyUnit);
		}

		private static Vector3 GetHeadPosition(UnitCore core)
		{
			if (core.HealthBarPoint != null)
				return core.HealthBarPoint.position + Vector3.up * HeadOffset;
			float height = core.Agent != null ? core.Agent.height * Mathf.Abs(core.transform.lossyScale.y) + 0.3f : 2.2f;
			return core.transform.position + Vector3.up * height;
		}

		private static void Despawn(ParticleSystem visual)
		{
			if (visual == null)
				return;
			visual.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ObjectPoolUtility<ParticleSystem>.Despawn(visual);
		}
	}
}
