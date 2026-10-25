using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Unit.Components
{
	public struct UnitHasDeathAnimMarker : IComponent
	{
		public bool instantDespawnAfterAnim;

		public ParticleSystem deathParticle;
	}
}
