using System;
using Codebase.Core;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Pet.Systems
{
	// Like PlayerRegenSystem, without the player's ability regen multiplier.
	public class PetRegenSystem : SystemBase
	{
		private Filter filter;

		[Inject]
		private Stash<UnitComponent> unitStash;

		public override void OnAwake()
		{
			filter = Filter.With<UnitComponent>().With<PetTag>().Without<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!PetConfig.Enabled)
				return;
			foreach (Entity entity in filter)
			{
				UnitCore unit = unitStash.Get(entity).Core;
				if (unit.IsDead)
					continue;
				double maximum = unit.GetStatValue(StatType.Health);
				unit.CurrentHealth = Math.Min(unit.CurrentHealth + unit.GetStatValue(StatType.HealthRegen) * deltaTime, maximum);
			}
		}
	}
}
