using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Vfx.Components
{
	public struct WorldVfxComponent : IComponent
	{
		public ParticleSystem ParticleSystem;

		public float Lifetime;

		public float Elapsed;
	}
}
