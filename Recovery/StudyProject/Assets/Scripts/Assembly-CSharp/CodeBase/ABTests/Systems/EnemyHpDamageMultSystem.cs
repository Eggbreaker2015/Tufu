using System;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.ABTests.Systems
{
	public class EnemyHpDamageMultSystem : SystemBase
	{
		[Inject]
		private ABTestModel abTestModel;

		[Inject]
		private Stash<UnitComponent> unitStash;

		private Filter enemyFilter;

		private string currentCohort;

		private EnemyHpDamageMultData currentData;

		public override void OnAwake()
		{
			enemyFilter = Filter.With<EnemyTag>().With<UnitComponent>().Build();
			currentCohort = abTestModel.GetVariant(ABKeys.EnemyHpDamageMult);
			currentData = ABTestData.EnemyHpDamageMult.GetCurrentValue();
		}

		public override void OnUpdate(float deltaTime)
		{
			string cohort = abTestModel.GetVariant(ABKeys.EnemyHpDamageMult);
			if (cohort == currentCohort) return;
			EnemyHpDamageMultData nextData = ABTestData.EnemyHpDamageMult.GetCurrentValue();
			foreach (var entity in enemyFilter)
			{
				if (!(unitStash.Get(entity).Core is EnemyUnit enemy) || enemy.Config.IsEvent) continue;
				float previous = GetMultiplier(currentData, enemy.Config.LocationNumber);
				float next = GetMultiplier(nextData, enemy.Config.LocationNumber);
				RecalculateStats(enemy, next / previous);
			}
			currentCohort = cohort;
			currentData = nextData;
		}

		private static float GetMultiplier(EnemyHpDamageMultData data, int locationNumber)
		{
			ValueByLocation value = data.damageMultiplier;
			return value.overridesByLocation != null && value.overridesByLocation.TryGetValue(locationNumber, out float result) ? result : value.defaultValue;
		}

		private void RecalculateStats(EnemyUnit enemy, float ratio)
		{
			double maximum = enemy.GetStatValue(StatType.Health);
			bool wasFull = maximum <= 0.0 || enemy.CurrentHealth / maximum >= 1.0;
			for (int i = 0; i < enemy.Stats.Count; i++)
			{
				Stat stat = enemy.Stats[i];
				if (stat.Type == StatType.Health || stat.Type == StatType.Damage)
					enemy.Stats[i] = new Stat(stat.Lifetime, Math.Round(stat.BaseValue * ratio), stat.Type);
			}
			enemy.BuildStatLookup();
			double updated = enemy.GetStatValue(StatType.Health);
			if (wasFull) enemy.CurrentHealth = updated;
		}
	}
}
