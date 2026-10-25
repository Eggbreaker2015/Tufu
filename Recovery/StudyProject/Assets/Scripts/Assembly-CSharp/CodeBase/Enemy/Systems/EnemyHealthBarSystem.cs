using System;
using Codebase.Core;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EnemyHealthBarSystem : SystemBase
	{
		[Serializable]
		private struct HealthBarSystemStateComponent : ISystemStateComponent, IComponent
		{
			public UnitCore Unit;
		}

		private SystemStateProcessor<HealthBarSystemStateComponent> unitProcessor;
		[Inject]
		private Stash<UnitComponent> unitStash;
		public override void OnAwake()
		{
			unitProcessor = Filter.With<UnitComponent>().With<EnemyTag>().Without<UnitDeadMarker>().Without<FarOnScreenMarker>().ToSystemStateProcessor<HealthBarSystemStateComponent>(CreateHealthBarForUnit, RemoveHealthBarForUnit);
		}

		public override void Dispose()
		{
			unitProcessor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			unitProcessor.Process();
		}

		private HealthBarSystemStateComponent CreateHealthBarForUnit(Entity entity)
		{
			var unit = (EnemyUnit)unitStash.Get(entity).Core;
			unit.SetupHealthBar(unit.healthBarPrefab);
			return new HealthBarSystemStateComponent
			{
				Unit = unit
			};
		}

		private void RemoveHealthBarForUnit(ref HealthBarSystemStateComponent state)
		{
			state.Unit.Dispose();
		}
	}
}
