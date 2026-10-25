using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Services.Time;
using Scellecs.Morpeh;
using UnityEngine;
using System;

namespace Codebase.Enemy.Systems
{
	public class EnemySpawnerBootstrapSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private EnemiesModel enemiesModel;
		[Inject]
		private ITimeService timeService;
		private const float NearbyBootstrapRange = 65f;
		private const float NearbyBootstrapRangeSq = 4225f;
		private const int MaxBootstrapsPerFrame = 6;
		private bool completedNearbyBootstrap;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (enemiesModel.PendingBootstrap.Count > 0)
				ProcessPendingSpawners(enemiesModel.PendingBootstrap);
			else if (enemiesModel.PendingEventMobBootstrap.Count > 0)
				ProcessPendingSpawners(enemiesModel.PendingEventMobBootstrap, true);
		}

		private void ProcessPendingSpawners(List<EnemySpawner> pending, bool skipNearbyPass = false)
		{
			var now = (int)new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			if (!skipNearbyPass && !completedNearbyBootstrap)
			{
				if (!TryGetPlayerReferencePosition(out var reference))
					return;
				for (var i = pending.Count - 1; i >= 0; --i)
				{
					var spawner = pending[i];
					if (spawner == null)
						pending.RemoveAt(i);
					else if ((spawner.transform.position - reference).sqrMagnitude <= NearbyBootstrapRangeSq)
					{
						spawner.CompleteBootstrap(now);
						pending.RemoveAt(i);
					}
				}

				completedNearbyBootstrap = true;
			}

			var remaining = MaxBootstrapsPerFrame;
			for (var i = pending.Count - 1; i >= 0 && remaining > 0; --i, --remaining)
			{
				var spawner = pending[i];
				if (spawner != null)
					spawner.CompleteBootstrap(now);
				pending.RemoveAt(i);
			}
		}

		private bool TryGetPlayerReferencePosition(out Vector3 position)
		{
			if (playerModel.Unit != null)
			{
				position = playerModel.Unit.transform.position;
				return true;
			}

			if (playerModel.LastPosition != Vector3.zero)
			{
				position = playerModel.LastPosition;
				return true;
			}

			position = default;
			return false;
		}
	}
}
