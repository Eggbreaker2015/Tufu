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

		// Prefab scale before the character-size factor; zero for fixed-position spawns.
		public Vector3 BaseScale;
	}
}
