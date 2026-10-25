using System;
using UnityEngine;

namespace FIMSpace.FTools
{
	[Serializable]
	public abstract class FIK_ProcessorBase
	{
		[Range(0f, 1f)]
		public float IKWeight;

		public Vector3 IKTargetPosition;

		public Quaternion IKTargetRotation;

		public Vector3 LastLocalDirection;

		public Vector3 LocalDirection;

		[NonSerialized]
		public bool CallPreCalibrate;

		public float fullLength { get; protected set; }

		public bool Initialized { get; protected set; }

		public FIK_IKBoneBase[] Bones { get; protected set; }

		public FIK_IKBoneBase StartBone => null;

		public FIK_IKBoneBase EndBone => null;

		public Quaternion StartBoneRotationOffset { get; set; }

		public virtual void Init(Transform root)
		{
		}

		public virtual void PreCalibrate()
		{
		}

		public virtual void Update()
		{
		}

		public static float EaseInOutQuint(float start, float end, float value)
		{
			return 0f;
		}
	}
}
