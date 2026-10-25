using Codebase.Core;
using Codebase.Player.Models;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Player.Distance
{
	public class PlayerDistanceTrackerSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private DistanceTraveledModel distanceModel;

		private Vector3 lastPosition;

		private bool hasLastPosition;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
            // Original ELF VA 0x1467b38. Track horizontal XZ Transform movement independently of deltaTime.
            distanceModel.SetLastFrameDelta(0f);
            if (playerModel.Unit == null) { hasLastPosition = false; return; }
            Vector3 position = playerModel.Unit.transform.position;
            if (!hasLastPosition)
            {
                lastPosition = position;
                hasLastPosition = true;
                return;
            }
            float x = position.x - lastPosition.x;
            float z = position.z - lastPosition.z;
            lastPosition = position;
            float distance = Mathf.Sqrt(x * x + z * z);
            if (distance > 0f) distanceModel.SetLastFrameDelta(distance);
        }
	}
}
