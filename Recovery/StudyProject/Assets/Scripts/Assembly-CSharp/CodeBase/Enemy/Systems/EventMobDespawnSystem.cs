using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EventMobDespawnSystem : SystemBase
	{
		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<EventDespawnRequest> eventDespawnRequestStash;

		private Filter filter;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
