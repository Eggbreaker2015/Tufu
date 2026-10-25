using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using System;

namespace Codebase.BossTower.Systems
{
	public class UnitHealthSyncSystem : SystemBase
	{
		private Filter filter;
		[Inject]
		private Stash<UnitHealthSyncComponent> trackerStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		public override void OnAwake()
		{
			filter = Filter.With<UnitHealthSyncComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (Entity entity in filter)
			{
				ref UnitHealthSyncComponent tracker = ref trackerStash.Get(entity);
				var unit = unitStash.Get(entity).Core;
				double maximum = unit.GetStatValue(Codebase.Unit.StatType.Health, true);
				if (unit.IsDead)
					continue;
				if (tracker.PrevMaxHp > 0.0 && Math.Abs(maximum - tracker.PrevMaxHp) > 0.001)
					unit.CurrentHealth = Math.Max(1.0, maximum - (tracker.PrevMaxHp - unit.CurrentHealth));
				tracker.PrevMaxHp = maximum;
			}
		}
	}
}
