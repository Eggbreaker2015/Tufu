using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Systems
{
	public class MarkDecaySystem : SystemBase
	{
		public const float DecayDuration = 12f;

		private Filter filter;

		private Stash<MarkComponent> markStash;

		public override void OnAwake()
		{
            // Native 0x15eb8f0: permanent markers never enter the original decay filter.
            markStash = World.GetStash<MarkComponent>();
            filter = World.Filter.With<MarkComponent>().Without<MarkPermanentMarker>().Build();
        }

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x15eba30; clear allow-above only after returning to cap, unless expired first.
            foreach (var entity in filter) {
                ref MarkComponent mark = ref markStash.Get(entity);
                mark.DecayTimer -= deltaTime;
                if (mark.DecayTimer <= 0f) markStash.Remove(entity);
                else if (mark.AllowToStayAboveMax && mark.Stacks <= mark.MaxStacks) mark.AllowToStayAboveMax = false;
            }
        }
	}
}
