using System;
using Codebase.Unit;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class PlayerRegenSystem : SystemBase
	{
		private Filter filter;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private PlayerModel playerModel;

		public override void OnAwake()
		{
            // Native 0x145574c: initialize actual matched living players to their maximum health once.
            filter = Filter.With<UnitComponent>().With<PlayerTag>().Without<UnitDeadMarker>().Build();
            foreach (var entity in filter) {
                var unit = unitStash.Get(entity).Core;
                unit.CurrentHealth = unit.GetStatValue(StatType.Health);
            }
        }

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x1455978: double stat * float ability scalar * float delta, upper cap only.
            foreach (var entity in filter) {
                var unit = unitStash.Get(entity).Core;
                double maximum = unit.GetStatValue(StatType.Health);
                unit.CurrentHealth = Math.Min(unit.CurrentHealth + unit.GetStatValue(StatType.HealthRegen) * playerModel.AbilityHealthRegenMultiplier * deltaTime, maximum);
            }
        }
	}
}
