using System;
using Codebase.Core;
using Codebase.Player;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Pet.Systems
{
	// Living pets get the player's health bar prefab; it goes away on death and comes back on revive.
	public class PetHealthBarSystem : SystemBase
	{
		[Serializable]
		private struct PetHealthBarStateComponent : ISystemStateComponent, IComponent
		{
			public UnitCore Unit;
		}

		private SystemStateProcessor<PetHealthBarStateComponent> unitProcessor;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private PlayerConfig playerConfig;

		public override void OnAwake()
		{
			unitProcessor = Filter.With<UnitComponent>().With<PetTag>().Without<UnitDeadMarker>().ToSystemStateProcessor<PetHealthBarStateComponent>(CreateHealthBarForUnit, RemoveHealthBarForUnit);
		}

		public override void Dispose()
		{
			unitProcessor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			unitProcessor.Process();
		}

		private PetHealthBarStateComponent CreateHealthBarForUnit(Entity entity)
		{
			UnitCore unit = unitStash.Get(entity).Core;
			if (unit.HealthBar == null)
				unit.SetupHealthBar(playerConfig.HealthBarPrefab);
			return new PetHealthBarStateComponent
			{
				Unit = unit
			};
		}

		private void RemoveHealthBarForUnit(ref PetHealthBarStateComponent state)
		{
			// A despawned pet has already released its bar (PetSpawnSystem); the pool must not get it twice.
			UnitCore unit = state.Unit;
			if (unit == null || unit.HealthBar == null)
				return;
			unit.Dispose();
			unit.HealthBar = null;
		}
	}
}
