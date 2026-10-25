using Codebase.ABTests;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Meta;
using Codebase.Services.Time;
using Codebase.Simulation.Systems;
using Scellecs.Morpeh;
using System;

namespace Codebase.Enemy.Systems
{
	public class EnemySpawnSystem : SystemBase
	{
		[Inject]
		private Stash<EnemySpawnerComponent> spawnerStash;
		[Inject]
		private Stash<EventSpawnerInactiveTag> inactiveStash;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private SimulationSystem simulationSystem;
		[Inject]
		private ABTestService abTestService;
		[Inject]
		private MetaStatsModel metaStatsModel;
		private Filter spawnerFilter;
		private MetaStat itemChargesStat;
		public override void OnAwake()
		{
			spawnerFilter = Filter.With<EnemySpawnerComponent>().Build();
			itemChargesStat = metaStatsModel.Get("items_rarity_charges");
		}

		public override void OnUpdate(float deltaTime)
		{
			if (simulationSystem.IsRunning)
				return;
			int now = (int)new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			int cooldownVersion = abTestService.SpawnerCooldownsVersion;
			int itemVersion = itemChargesStat != null ? itemChargesStat.ModifierVersion : 0;
			foreach (Entity entity in spawnerFilter)
			{
				if (inactiveStash.Has(entity))
					continue;
				EnemySpawner spawner = spawnerStash.Get(entity).Spawner;
				if (spawner.IsEnabled)
					spawner.ProcessSpawn(now, cooldownVersion, itemVersion);
			}
		}
	}
}
