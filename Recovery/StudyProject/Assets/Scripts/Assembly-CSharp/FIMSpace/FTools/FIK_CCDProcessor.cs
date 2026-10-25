using System;
using UnityEngine;

namespace FIMSpace.FTools
{
	[Serializable]
	public class FIK_CCDProcessor : FIK_ProcessorBase
	{
		[Serializable]
		public class CCDIKBone : FIK_IKBoneBase
		{
			[Range(0f, 180f)]
			public float AngleLimit;

			[Range(0f, 180f)]
			public float TwistAngleLimit;

			public Vector3 ForwardOrientation;

			public float FrameWorldLength;

			public Vector2 HingeLimits;

			public Quaternion PreviousHingeRotation;

			public float PreviousHingeAngle;

			public Vector3 LastIKLocPosition;

			public Quaternion LastIKLocRotation;

			public CCDIKBone IKParent { get; private set; }

			public CCDIKBone IKChild { get; private set; }

			public CCDIKBone(Transform t)
				: base(null)
			{
			}

			public void Init(CCDIKBone child, CCDIKBone parent)
			{
			}

			public override void SetChild(FIK_IKBoneBase child)
			{
			}

			public void AngleLimiting()
			{
			}

			private Quaternion LimitSpherical(Quaternion rotation)
			{
				return default;
			}

			private Quaternion LimitZ(Quaternion currentRotation)
			{
				return default;
			}

			private Quaternion LimitHinge(Quaternion rotation)
			{
				return default;
			}
		}

		public CCDIKBone[] IKBones;

		public bool ContinousSolving;

		[Range(0f, 1f)]
		public float SyncWithAnimator;

		[Range(1f, 12f)]
		public int ReactionQuality;

		[Range(0f, 1f)]
		public float Smoothing;

		[Range(0f, 1.5f)]
		public float StretchToTarget;

		public AnimationCurve StretchCurve;

		public bool Use2D;

		public bool Invert;

		public CCDIKBone StartIKBone => null;

		public CCDIKBone EndIKBone => null;

		public float ActiveLength { get; private set; }

		public FIK_CCDProcessor(Transform[] bonesChain)
		{
		}

		public override void Init(Transform root)
		{
		}

		public override void Update()
		{
		}

		protected Vector3 GetGoalPivotOffset()
		{
			return default;
		}

		private bool GoalPivotOffsetDetected()
		{
			return false;
		}

		private Vector3 RefreshLocalDirection()
		{
			return default;
		}

		private float GetVelocityDifference()
		{
			return 0f;
		}

		public void AutoLimitAngle(float angleLimit = 60f, float twistAngleLimit = 50f)
		{
		}

		public void AutoWeightBones(float baseValue = 1f)
		{
		}

		public void AutoWeightBones(AnimationCurve weightCurve)
		{
		}
	}
}
