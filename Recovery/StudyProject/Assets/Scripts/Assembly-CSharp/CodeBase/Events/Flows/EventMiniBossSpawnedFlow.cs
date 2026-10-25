using Codebase.Core.Flows;
using UnityEngine;

namespace Codebase.Events.Flows
{
	public struct EventMiniBossSpawnedFlow : IFlow
	{
		public Vector3 TargetPosition;

		public Transform TargetTransform;
	}
}
