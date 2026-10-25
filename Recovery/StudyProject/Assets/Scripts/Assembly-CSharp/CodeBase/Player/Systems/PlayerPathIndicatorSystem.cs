using Codebase.Core;
using Codebase.Events.Models;
using Codebase.Gates;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Player.Systems
{
	public class PlayerPathIndicatorSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private GatesModel gatesModel;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private Stash<UnitComponent> unitStash;

		private Filter enemyFilter;

		private GameObject indicator;

		private NavMeshPath navMeshPath;

		private Vector3[] pathCorners;

		private const float MinDistance = 2f;

		private const float MaxDistance = 3f;

		private const float OscillationSpeed = 2f;

		private const float MinPathPointDistance = 1f;

		private const float PathUpdateInterval = 0.5f;

		private const float DirectionSmoothSpeed = 3f;

		private const float LookAheadDistance = 5f;

		private const float MinMiniBossShowDistance = 10f;

		private float timer;

		private float pathUpdateTimer;

		private Vector3 currentDirection;

		private Vector3 targetDirection;

		private bool hasDirection;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private Vector3? TryGetEventMiniBossPosition(Vector3 playerPosition)
		{
			return null;
		}

		private Vector3? TryGetGatePosition(Vector3 playerPosition, int targetLocationNumber)
		{
			return null;
		}

		private void UpdateIndicator(float deltaTime, Vector3 playerPosition, Vector3 targetPosition)
		{
		}

		private Vector3 GetPathDirection(Vector3 from, Vector3 to)
		{
			return default;
		}
	}
}
