using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Utilities;

namespace Codebase.Enemy.Systems
{
	public class EnemySpawnerCooldownSystem : SystemBase
	{
		[Inject]
		private Stash<EnemySpawnerComponent> spawnerStash;
		[Inject]
		private Stash<EnemySpawnerOnCooldownComponent> cooldownStash;
		[Inject]
		private Stash<PositionComponent> positionStash;
		[Inject]
		private Stash<FarFromPlayerMarker> farFromPlayerMarkerStash;
		[Inject]
		private Stash<FarOnScreenMarker> farOnScreenMarkerStash;
		[Inject]
		private PlayerModel playerModel;
		private const float FarDistance = 3600f;
		private const float FarOnScreenDistance = 900f;
		private Filter spawnerFilter;
		public override void OnAwake()
		{
			spawnerFilter = Filter.With<EnemySpawnerComponent>().With<PositionComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			var playerPosition = playerModel.Unit.transform.position;
			foreach (Entity entity in spawnerFilter)
			{
				EnemySpawner spawner = spawnerStash.Get(entity).Spawner;
				float distanceSq = (playerPosition - positionStash.Get(entity).Position).sqrMagnitude;
				farFromPlayerMarkerStash.SetIf(entity, distanceSq >= 3600f);
				farOnScreenMarkerStash.SetIf(entity, distanceSq >= 900f);
				if (!spawner.AnyoneAlive)
					cooldownStash.SetIf(entity, spawner.Cooldown > 0f && (!spawner.Config.DisableAfterFirstClear || !spawner.HasBeenClearedOnce));
				else
					cooldownStash.Remove(entity);
			}
		}
	}
}
