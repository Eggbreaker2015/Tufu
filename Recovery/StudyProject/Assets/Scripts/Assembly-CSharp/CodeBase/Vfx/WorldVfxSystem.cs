using Codebase.Core;
using Codebase.Vfx.Components;
using Scellecs.Morpeh;
using UnityEngine;
using Codebase.Utilities.ObjectPool;

namespace Codebase.Vfx
{
	public class WorldVfxSystem : SystemBase
	{
		private const float ArrivalThreshold = 0.1f;
		private Filter filter;
		private Filter travelFilter;
		private Filter forceFieldFilter;
		[Inject]
		private Stash<WorldVfxComponent> vfxStash;
		[Inject]
		private Stash<WorldVfxTravelComponent> travelStash;
		[Inject]
		private Stash<WorldVfxForceFieldComponent> forceFieldStash;
		public override void OnAwake()
		{
			filter = Filter.With<WorldVfxComponent>().Build();
			travelFilter = Filter.With<WorldVfxTravelComponent>().Build();
			forceFieldFilter = Filter.With<WorldVfxForceFieldComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (Entity entity in filter)
			{
				ref WorldVfxComponent vfx = ref vfxStash.Get(entity);
				float lifetime = vfx.Lifetime;
				vfx.Elapsed += deltaTime;
				bool finished = vfx.Elapsed > 0f && !vfx.ParticleSystem.IsAlive(true);
				if (vfx.Elapsed >= lifetime || finished)
				{
					ObjectPoolUtility<ParticleSystem>.Despawn(vfx.ParticleSystem);
					World.RemoveEntity(entity);
				}
			}

			foreach (Entity entity in travelFilter)
			{
				ref WorldVfxTravelComponent travel = ref travelStash.Get(entity);
				if (travel.Target == null)
				{
					ObjectPoolUtility<ParticleSystem>.Despawn(travel.ParticleSystem);
					World.RemoveEntity(entity);
					continue;
				}

				Vector3 target = travel.Target.position + travel.Offset;
				Vector3 position = travel.ParticleSystem.transform.position;
				if (Vector3.Distance(position, target) <= ArrivalThreshold)
				{
					travel.OnArrived?.Invoke();
					ObjectPoolUtility<ParticleSystem>.Despawn(travel.ParticleSystem);
					World.RemoveEntity(entity);
				}
				else
					travel.ParticleSystem.transform.position = Vector3.MoveTowards(position, target, travel.Speed * deltaTime);
			}

			foreach (Entity entity in forceFieldFilter)
			{
				ref WorldVfxForceFieldComponent vfx = ref forceFieldStash.Get(entity);
				float lifetime = vfx.Lifetime;
				vfx.Elapsed += deltaTime;
				bool finished = vfx.Elapsed > 0f && !vfx.ParticleSystem.IsAlive(true);
				if (vfx.Elapsed >= lifetime || finished)
				{
					ParticleSystem.ExternalForcesModule external = vfx.ParticleSystem.externalForces;
					external.RemoveAllInfluences();
					ObjectPoolUtility<ParticleSystemForceField>.Despawn(vfx.ForceField);
					ObjectPoolUtility<ParticleSystem>.Despawn(vfx.ParticleSystem);
					World.RemoveEntity(entity);
				}
			}
		}
	}
}
