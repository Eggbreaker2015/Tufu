using Codebase.Core;
using Codebase.Player.Models;
using Codebase.SecretChest.Components;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.SecretChest.Systems
{
	public class SecretChestProximitySystem : SystemBase
	{
		private const float HideBuffer = 1f;

		private const float SampleRadius = 3f;

		private const float UpdateInterval = 0.15f;

		[Inject]
		private Stash<SecretChestComponent> chestStash;

		[Inject]
		private Stash<SecretChestNearMarker> nearMarkerStash;

		[Inject]
		private PlayerModel playerModel;

		private Filter filter;

		private NavMeshPath path;

		private float updateTimer;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private float PathLength()
		{
			return 0f;
		}

		private bool IsWithinNavMeshDistance(Vector3 from, Vector3 to, float maxDistance)
		{
			return false;
		}
	}
}
