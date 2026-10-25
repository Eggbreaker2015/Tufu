using DG.Tweening;
using UnityEngine;

namespace Codebase.Player
{
	[CreateAssetMenu(fileName = "WeaponAttackAnimationSettings", menuName = "Codebase/Player/Weapon Attack Animation Settings")]
	public class WeaponAttackAnimationSettings : ScriptableObject
	{
		[SerializeField]
		public Vector3 AttackTargetPositionOffset;

		[field: SerializeField]
		[field: Min(0.05f)]
		public float AnimationDurationCap { get; private set; }

		[field: SerializeField]
		public Vector3 TravelEndPositionOffset { get; private set; }

		[field: SerializeField]
		public Vector3 WindupOffset { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 180f)]
		public float AngleOffset { get; private set; }

		[field: SerializeField]
		public WeaponWindupOffsetSpace WindupOffsetSpace { get; private set; }

		[field: SerializeField]
		public WeaponLocalAxisDirection WeaponTopAxis { get; private set; }

		[field: SerializeField]
		public WeaponLocalAxisDirection WeaponBladeAxis { get; private set; }

		[field: SerializeField]
		[field: Min(0f)]
		public float OvertravelRatio { get; private set; }

		[field: SerializeField]
		[field: Range(-180f, 180f)]
		public float OvertravelDirectionAngle { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float WindupRatio { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float TravelRatio { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float ReturnRatio { get; private set; }

		[field: SerializeField]
		public WeaponTravelType WindupTravelType { get; private set; }

		[field: SerializeField]
		public WeaponPathFormula WindupPathFormula { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float WindupStraightening { get; private set; }

		[field: SerializeField]
		public Ease WindupEase { get; private set; }

		[field: SerializeField]
		public Ease WindupScaleEase { get; private set; }

		[field: SerializeField]
		public Ease WindupRotationEase { get; private set; }

		[field: SerializeField]
		public WeaponTravelType TravelTravelType { get; private set; }

		[field: SerializeField]
		public WeaponPathFormula TravelPathFormula { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float TravelStraightening { get; private set; }

		[field: SerializeField]
		public Ease TravelEase { get; private set; }

		[field: SerializeField]
		public Ease TravelScaleEase { get; private set; }

		[field: SerializeField]
		public Ease TravelRotationEase { get; private set; }

		[field: SerializeField]
		public WeaponTravelType OvertravelTravelType { get; private set; }

		[field: SerializeField]
		public WeaponPathFormula OvertravelPathFormula { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float OvertravelStraightening { get; private set; }

		[field: SerializeField]
		public Ease OvertravelEase { get; private set; }

		[field: SerializeField]
		public Ease OvertravelScaleEase { get; private set; }

		[field: SerializeField]
		public Ease OvertravelRotationEase { get; private set; }

		[field: SerializeField]
		public WeaponTravelType ReturnTravelType { get; private set; }

		[field: SerializeField]
		public WeaponPathFormula ReturnPathFormula { get; private set; }

		[field: SerializeField]
		[field: Range(0f, 1f)]
		public float ReturnStraightening { get; private set; }

		[field: SerializeField]
		public Ease ReturnEase { get; private set; }

		[field: SerializeField]
		public Ease ReturnScaleEase { get; private set; }

		[field: SerializeField]
		public Ease ReturnRotationEase { get; private set; }

		[field: SerializeField]
		public AttackPoseSettings Windup { get; private set; }

		[field: SerializeField]
		public AttackPoseSettings Hit { get; private set; }

		[field: SerializeField]
		public AttackPoseSettings Overtravel { get; private set; }
	}
}
