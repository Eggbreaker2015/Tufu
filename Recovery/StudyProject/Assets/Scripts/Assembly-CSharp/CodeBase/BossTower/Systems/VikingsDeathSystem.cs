using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Enemy;

namespace Codebase.BossTower.Systems
{
	public class VikingsDeathSystem : SystemBase
	{
		private Filter deadFilter;
		private Filter vikingsFilter;
		private Filter raidFilter;
		private HashSet<int> processedDeaths = new HashSet<int>();
		[Inject]
		private VikingsBuffSystem vikingsBuffSystem;
		[Inject]
		private VikingsRageDebuffSystem vikingsRageDebuffSystem;
		[Inject]
		private VikingsGhostSystem vikingsGhostSystem;
		[Inject]
		private Stash<VikingsBossComponent> vikingsStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<BossFightState> bossFightState;
		[Inject]
		private Stash<BossRaidComponent> raidStash;
		public override void OnAwake()
		{
			deadFilter = Filter.With<VikingsBossComponent>().With<UnitComponent>().With<UnitDeadMarker>().Build();
			vikingsFilter = Filter.With<VikingsBossComponent>().With<UnitComponent>().Build();
			raidFilter = Filter.With<BossRaidComponent>().With<PlayerTag>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (Entity entity in deadFilter)
			{
				int key = entity.Id.GetHashCode();
				if (processedDeaths.Contains(key) || bossFightState.IsEmpty())
					continue;
				processedDeaths.Add(key);
				VikingsBossComponent data = vikingsStash.Get(entity);
				EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
				int count = IncrementDeadAlliesCount();
				vikingsBuffSystem.OnVikingDied(data.GroupId, data, count, unit.transform, data.BuffVfxPrefab, data.BuffForceFieldPrefab);
				vikingsRageDebuffSystem.OnVikingDied();
				vikingsGhostSystem.OnVikingDied(unit, data, count);
			}
		}

		private int IncrementDeadAlliesCount()
		{
			foreach (Entity entity in raidFilter)
				return ++raidStash.Get(entity).DeadAlliesCount;
			return 0;
		}

		public void OnRaidEnded()
		{
			processedDeaths.Clear();
			foreach (Entity entity in vikingsFilter)
				vikingsStash.Remove(entity);
		}
	}
}
