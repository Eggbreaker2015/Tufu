using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Unit.Systems
{
	public class NavMeshSyncSystem : SystemBase
	{
		[Inject]
		private Stash<PositionComponent> positionStash;

		[Inject]
		private Stash<NavMeshAgentComponent> navAgentStash;

		private Filter filter;

		public override void OnAwake()
		{
			filter = World.Filter.With<NavMeshAgentComponent>().With<NavMeshAgentActiveMarker>().With<PositionComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (var entity in filter)
			    positionStash.Get(entity).Position = navAgentStash.Get(entity).Agent.nextPosition;
		}
	}
}
