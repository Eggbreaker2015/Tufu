using Codebase.Core;
using Codebase.Particles.Components;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Particles.Systems
{
	public class ParticlesVisibilitySystem : SystemBase
	{
		private const float FarDistance = 80f;

		private const float FarDistanceSqr = 6400f;

		private Filter particlesFilter;

		private Filter playerFilter;

		[Inject]
		private Stash<PositionComponent> positionStash;

		[Inject]
		private Stash<ParticleSystemComponent> particleStash;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
