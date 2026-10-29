using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EnemyRegenSystem : SystemBase
	{
		private Filter filter;
		[Inject]
		private Stash<UnitComponent> unitStash;
		private const double HEAL_PER_SECOND = 0.05;
		private const double EVADE_HEAL_PER_SECOND = 0.25;
		private const int NO_DAMAGE_TIME_TO_HEAL = 5;
		public override void OnAwake()
		{
			filter = Filter.With<UnitComponent>().With<EnemyTag>().Without<UnitDeadMarker>().Without<FarFromPlayerMarker>().Without<UnitWithoutRegenMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (Entity entity in filter)
			{
				var unit = unitStash.Get(entity).Core;
				var enemy = unit as EnemyUnit;
				double maxHealth = unit.GetStatValue(Codebase.Unit.StatType.Health);
				double regen = unit.GetStatValue(Codebase.Unit.StatType.HealthRegen);
				double idleRegen = enemy != null && enemy.IsReturningHome ? maxHealth * EVADE_HEAL_PER_SECOND :
					unit.LastDamageGet + NO_DAMAGE_TIME_TO_HEAL <= UnityEngine.Time.time ? maxHealth * HEAL_PER_SECOND : 0.0;
				unit.CurrentHealth = System.Math.Min(unit.CurrentHealth + (regen + idleRegen) * deltaTime, maxHealth);
			}
		}
	}
}
