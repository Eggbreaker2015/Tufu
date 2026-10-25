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
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
