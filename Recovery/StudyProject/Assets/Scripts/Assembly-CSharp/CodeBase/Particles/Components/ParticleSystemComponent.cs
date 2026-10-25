using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Particles.Components
{
	public struct ParticleSystemComponent : IComponent
	{
		public ParticleSystem ParticleSystem;

		public bool VisibilityInitialized;

		public bool IsFar;
	}
}
