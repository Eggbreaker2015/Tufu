using Codebase.Events;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Events.Models;
using Codebase.Events.Systems;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EventSpawnerControlSystem : SystemBase
	{
		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventBossGateSystem eventBossGateSystem;

		[Inject]
		private Stash<EnemySpawnerComponent> spawnerStash;

		[Inject]
		private Stash<EventSpawnerTag> eventSpawnerStash;

		[Inject]
		private Stash<EventSpawnerInactiveTag> inactiveStash;

		private Filter eventSpawnerFilter;

		public override void OnAwake()
		{
			eventSpawnerFilter = Filter.With<EnemySpawnerComponent>().With<EventSpawnerTag>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (var entity in eventSpawnerFilter)
			{
				var model = eventsRepository.Get(eventSpawnerStash.Get(entity).EventKey);
				var inactive = inactiveStash.Has(entity);
				var spawner = spawnerStash.Get(entity).Spawner;
				if (model == null) continue;
				if (model.State == EventState.Active && inactive)
				{
					inactiveStash.Remove(entity);
					spawner.MinimapVersion++;
				}
				else if (model.State != EventState.Active && !inactive)
				{
					inactiveStash.Add(entity);
					eventBossGateSystem.SetMiniBossFlowRunning(false);
					spawner.MarkAllUnitsForEventDespawn();
				}
			}
		}
	}
}
