using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Abilities.Vfx.Components
{
	public struct AbilityVfxComponent : IComponent
	{
		public ParticleSystem ParticleSystem;

		public Transform Target;

		public float Duration;

		public float Elapsed;

		public float HeightOffset;

		public Vector3 PositionOffset;
	}
}
