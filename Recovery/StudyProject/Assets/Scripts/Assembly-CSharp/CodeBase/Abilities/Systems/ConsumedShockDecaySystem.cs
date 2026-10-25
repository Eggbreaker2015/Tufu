using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Systems
{
	public class ConsumedShockDecaySystem : SystemBase
	{
		public const float DecayDuration = 10f;

		public const float ShockedDuration = 10f;

		private Filter filter;

		private Stash<ShockComponent> shockStash;

		public override void OnAwake()
		{
            // Native 0x15eb698.
            shockStash = World.GetStash<ShockComponent>();
            filter = World.Filter.With<ShockComponent>().Build();
        }

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x15eb784: consumed timer is paused while shocked; whole delta is used on the expiration frame.
            foreach (var entity in filter) {
                ref ShockComponent shock = ref shockStash.Get(entity);
                if (shock.IsShocked) {
                    shock.ShockedTimer -= deltaTime;
                    if (shock.ShockedTimer > 0f) continue;
                    shock.IsShocked = false; shock.ShockedTimer = 0f;
                }
                if (shock.ConsumedShockCount > 0) {
                    shock.ConsumedDecayTimer -= deltaTime;
                    if (shock.ConsumedDecayTimer <= 0f) shock.ConsumedShockCount = 0;
                }
            }
        }
	}
}
