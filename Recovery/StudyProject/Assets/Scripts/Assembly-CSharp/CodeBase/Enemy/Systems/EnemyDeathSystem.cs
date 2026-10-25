using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Utilities;

namespace Codebase.Enemy.Systems
{
	public class EnemyDeathSystem : SystemBase
	{
		private Filter filter;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<UnitDeadMarker> unitDeadMarkerStash;
		[Inject]
		private Stash<EventDespawnRequest> eventDespawnRequestStash;
		[Inject]
		private Stash<CreeperMobComponent> creeperStash;
		public override void OnAwake()
		{
			filter = Filter.With<UnitComponent>().With<EnemyTag>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (Entity entity in filter)
			{
				var unit = unitStash.Get(entity).Core;
				if (eventDespawnRequestStash.Has(entity))
					unitDeadMarkerStash.Set(entity);
				else
				{
					bool suppressed = creeperStash.Has(entity) && creeperStash.Get(entity).State != CreeperMobState.Finalized;
					unitDeadMarkerStash.SetIf(entity, unit.IsDead && !suppressed);
				}
			}
		}
	}
}
