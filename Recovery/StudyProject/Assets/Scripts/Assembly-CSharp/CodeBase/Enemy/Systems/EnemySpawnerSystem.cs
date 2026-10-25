using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Player.Models;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EnemySpawnerSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private EnemiesModel enemiesModel;
		[Inject]
		private Stash<EnemySpawnerComponent> enemySpawnerStash;
		private Filter enemyFilter;
		private int locationNumber;
		public override void OnAwake()
		{
			enemyFilter = Filter.With<EnemySpawnerComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			int current = playerModel.LastUnlockedLocation.Number;
			bool changed = locationNumber != current;
			if (changed)
				locationNumber = current;
			foreach (Entity entity in enemyFilter)
			{
				EnemySpawner spawner = enemySpawnerStash.Get(entity).Spawner;
				if (changed)
					spawner.MinimapVersion++;
			}
		}
	}
}
