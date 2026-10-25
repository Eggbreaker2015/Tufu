using Codebase.Core;
using Codebase.Quests.Components;
using Scellecs.Morpeh;

namespace Codebase.Quests.Systems
{
	public class QuestRoadVisibilitySystem : SystemBase
	{
		[Inject]
		private Stash<QuestRoadComponent> roadStash;

		[Inject]
		private QuestModel questModel;

		private Filter filter;

		public override void OnAwake()
		{
			// Original ELF 0x1431918.
			filter = Filter.With<QuestRoadComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ELF 0x14319cc.
			foreach (var entity in filter) {
			    ref var road = ref roadStash.Get(entity);
			    bool visible = questModel.Road.ShouldShowQuestRoad(road);
			    if (visible != road.Road.activeSelf) road.Road.SetActive(visible);
			}
		}
	}
}
