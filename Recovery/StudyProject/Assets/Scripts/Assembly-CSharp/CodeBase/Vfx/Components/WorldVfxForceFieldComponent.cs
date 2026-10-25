using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Vfx.Components
{
	public struct WorldVfxForceFieldComponent : IComponent
	{
		public ParticleSystem ParticleSystem;

		public ParticleSystemForceField ForceField;

		public float Lifetime;

		public float Elapsed;
	}
}
