using System;
using UnityEngine;

namespace Codebase.Player
{
	[Serializable]
	public struct WeaponAttackVfxSettings
	{
		public Transform Prefab;

		public WeaponAttackPhase SpawnPhase;

		public float PhaseProgress;

		public WeaponAttackVfxOffsetSpace OffsetSpace;

		public bool FollowWeaponUntilPhaseEndOrVfxEnd;

		[Min(0.01f)]
		public float Lifetime;

		public Vector3 PositionOffset;

		public Vector3 RotationOffset;

		public Vector3 ScaleOffset;
	}
}
