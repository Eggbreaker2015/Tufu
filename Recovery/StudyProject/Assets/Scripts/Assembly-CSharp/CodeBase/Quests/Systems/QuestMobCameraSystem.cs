using System;
using Codebase.Core;
using Codebase.Quests.Components;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Quests.Flows;
using Cysharp.Threading.Tasks;

namespace Codebase.Quests.Systems
{
	public class QuestMobCameraSystem : SystemBase
	{
		[Serializable]
		private struct QuestMobCameraState : ISystemStateComponent, IComponent
		{
			public Entity Entity;
		}

		private SystemStateProcessor<QuestMobCameraState> processor;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<QuestMobTag> mobTagStash;
		[Inject]
		private QuestMobSpawnerService questMobSpawnerService;
		public override void OnAwake()
		{ // Original ELF 0x142ec78; independent candidate only.
			processor = Filter.With<QuestMobTag>().With<UnitComponent>().Without<UnitDeadMarker>().ToSystemStateProcessor<QuestMobCameraState>(OnMobSpawned, OnMobRemoved);
		}

		public override void Dispose()
		{ // Original ELF 0x142ee70; independent candidate only.
			processor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{ // Original ELF 0x142eeb8; independent candidate only.
			processor.Process();
		}

		private QuestMobCameraState OnMobSpawned(Entity entity)
		{ // Original ELF 0x142ef00; independent candidate only.
			var tag = mobTagStash.Get(entity);
			if (tag.FocusCamera)
			{
				var unit = unitStash.Get(entity).Core;
				App.InitiateFlow(new QuestMobSpawnedFlow { TargetTransform = unit.transform, SpawnVfx = questMobSpawnerService.GetMobSpawnVfx(tag.StepKey) }).Forget();
			}

			return new QuestMobCameraState
			{
				Entity = entity
			};
		}

		private void OnMobRemoved(ref QuestMobCameraState state)
		{
		}
	}
}
