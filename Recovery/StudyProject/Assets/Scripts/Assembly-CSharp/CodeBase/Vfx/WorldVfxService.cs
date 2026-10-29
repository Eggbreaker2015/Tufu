using System;
using UnityEngine;
using Codebase.Utilities.ObjectPool;
using Codebase.Vfx.Components;
using Scellecs.Morpeh;

namespace Codebase.Vfx
{
	public class WorldVfxService
	{
		public ParticleSystem Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation, float lifetime, Transform parent = null)
		{
			if (prefab == null)
				return null;
			ParticleSystem particle = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, position, parent);
			particle.transform.rotation = rotation;
			if (parent != null)
				particle.transform.position = position;
			particle.Clear(true);
			particle.Play(true);
			World world = World.Default;
			Entity entity = world.CreateEntity();
			world.GetStash<WorldVfxComponent>().Set(entity, new WorldVfxComponent { ParticleSystem = particle, Lifetime = lifetime });
			return particle;
		}

		public ParticleSystem Spawn(ParticleSystem prefab, Transform parent, Vector3 localPosition, float lifetime)
		{
			if (prefab == null)
				return null;
			ParticleSystem particle = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, parent.position, parent);
			if (parent != null)
				particle.transform.localPosition = localPosition;
			particle.Clear(true);
			particle.Play(true);
			World world = World.Default;
			Entity entity = world.CreateEntity();
			world.GetStash<WorldVfxComponent>().Set(entity, new WorldVfxComponent { ParticleSystem = particle, Lifetime = lifetime });
			return particle;
		}

		public void SpawnWithForceField(ParticleSystem particlePrefab, ParticleSystemForceField forceFieldPrefab, Transform particleTransform, Transform forceFieldTransform, float lifetime, Vector3 forceFieldOffset = default(Vector3))
		{
			if (particlePrefab == null || forceFieldPrefab == null)
				return;
			ParticleSystemForceField force = ObjectPoolUtility<ParticleSystemForceField>.Spawn(forceFieldPrefab, forceFieldTransform.position + forceFieldOffset, forceFieldTransform);
			force.transform.rotation = forceFieldTransform.rotation;
			force.transform.localPosition = forceFieldOffset;
			ParticleSystem particle = ObjectPoolUtility<ParticleSystem>.Spawn(particlePrefab, particleTransform.position, particleTransform);
			particle.transform.rotation = particleTransform.rotation;
			ParticleSystem.ExternalForcesModule external = particle.externalForces;
			external.RemoveAllInfluences();
			external.enabled = true;
			external.AddInfluence(force);
			particle.Clear(true);
			particle.Play(true);
			World world = World.Default;
			Entity entity = world.CreateEntity();
			world.GetStash<WorldVfxForceFieldComponent>().Set(entity, new WorldVfxForceFieldComponent { ParticleSystem = particle, ForceField = force, Lifetime = lifetime });
		}

		// lifetime > 0: despawned after that many seconds without arriving (onArrived is not called).
		public void SpawnTracking(ParticleSystem prefab, Vector3 startPosition, Transform target, Vector3 offset, float speed, Action onArrived = null, float lifetime = 0f)
		{
			if (prefab == null || target == null)
				return;
			ParticleSystem particle = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, startPosition);
			particle.Clear(true);
			particle.Play(true);
			World world = World.Default;
			Entity entity = world.CreateEntity();
			world.GetStash<WorldVfxTravelComponent>().Set(entity, new WorldVfxTravelComponent { ParticleSystem = particle, Target = target, Offset = offset, Speed = speed, OnArrived = onArrived, Lifetime = lifetime });
		}
	}
}
