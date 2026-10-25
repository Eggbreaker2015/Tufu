using UnityEngine;

namespace Codebase.Player
{
	[CreateAssetMenu(fileName = "WeaponIdleAnimationSettings", menuName = "Codebase/Player/Weapon Idle Animation Settings")]
	public class WeaponIdleAnimationSettings : ScriptableObject
	{
		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float PositionLag { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float RotationLag { get; private set; }

		[field: SerializeField]
		public bool EnableLoop { get; private set; }

		[field: SerializeField]
		[field: Min(0f)]
		public float PositionOuterRadius { get; private set; }

		[field: SerializeField]
		[field: Min(0f)]
		public float PositionInnerRadius { get; private set; }

		[field: SerializeField]
		[field: Min(0.01f)]
		public float PositionLoopSpeed { get; private set; }

		[field: SerializeField]
		public bool EnableRotationLoop { get; private set; }

		[field: SerializeField]
		public Vector3 RotationMinEuler { get; private set; }

		[field: SerializeField]
		public Vector3 RotationMaxEuler { get; private set; }

		[field: SerializeField]
		[field: Min(1f)]
		public float RotationLoopSpeed { get; private set; }
	}
}
