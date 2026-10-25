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
			filter = Filter.With<UnitComponent>().With<EnemyTag>().With<UnitDeadMarker>().With<EventDespawnRequest>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (var entity in filter)
			{
				var unit = (EnemyUnit)unitStash.Get(entity).Core;
				var request = eventDespawnRequestStash.Get(entity);
				request.Spawner.ProcessEventDespawn(unit);
				eventDespawnRequestStash.Remove(entity);
			}
		}
	}
}
