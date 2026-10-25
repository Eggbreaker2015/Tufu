using Codebase.Camera;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Utilities;

namespace Codebase.Enemy.Systems
{
	public class EnemyVisibilitySystem : SystemBase
	{
		[Inject]
		private EnemiesModel enemiesModel;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		private const float FarDistance = 3600f;
		private const float FarOnScreenDistance = 900f;
		private Filter enemyFilter;
		private Filter playerFilter;
		[Inject]
		private Stash<PositionComponent> positionStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<FarFromPlayerMarker> farFromPlayerStash;
		[Inject]
		private Stash<FarOnScreenMarker> farOnScreenStash;
		[Inject]
		private Stash<IgnoreFarMarker> ignoreFarStash;
		[Inject]
		private Stash<NavMeshAgentActiveMarker> navMeshActiveStash;
		public override void OnAwake()
		{
			enemyFilter = Filter.With<UnitComponent>().With<PositionComponent>().With<EnemyTag>().Without<IgnoreFarMarker>().Build();
			playerFilter = Filter.With<UnitComponent>().With<PositionComponent>().With<PlayerTag>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			Entity player = default;
			bool found = false;
			foreach (Entity entity in playerFilter)
			{
				player = entity;
				found = true;
				break;
			}

			if (!found || player.IsNullOrDisposed())
				return;
			var playerPosition = positionStash.Get(player).Position;
			var attackPosition = cameraFollowSystem.PositionOfAttack;
			foreach (Entity entity in enemyFilter)
			{
				var position = positionStash.Get(entity).Position;
				float distanceSq = System.Math.Min((playerPosition - position).sqrMagnitude, (attackPosition - position).sqrMagnitude);
				var unit = unitStash.Get(entity).Core;
				bool far = distanceSq > FarDistance;
				unit.IsFar = far;
				if (!unit.DisableAgent)
				{
					bool shouldEnableAgent = !far && !unit.IsDead;
					if (shouldEnableAgent != navMeshActiveStash.Has(entity))
					{
						unit.Agent.enabled = shouldEnableAgent;
						navMeshActiveStash.SetIf(entity, shouldEnableAgent);
					}

					if (far == unit.VisualRootEnabled)
					{
						unit.VisualRootEnabled = !far;
						unit.VisualRoot.gameObject.SetActive(!far);
					}
				}

				if (far != farFromPlayerStash.Has(entity))
					farFromPlayerStash.SetIf(entity, far);
				bool farOnScreen = distanceSq > FarOnScreenDistance;
				if (farOnScreen != farOnScreenStash.Has(entity))
					farOnScreenStash.SetIf(entity, farOnScreen);
			}
		}
	}
}
