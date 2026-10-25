using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Enemy.Components
{
	[Serializable]
	public struct CreeperMobComponent : IComponent
	{
		public float ExplosionDelay;

		public float ExplosionRadius;

		public float DamageMaxHealthPercent;

		public ParticleSystem ExplosionPrepareParticle;

		public ParticleSystem ExplosionPrepareParticleInner;

		public ParticleSystem ExplosionVfxPrefab;

		public float ExplosionVfxLifetime;

		[NonSerialized]
		public CreeperMobState State;
	}
}
