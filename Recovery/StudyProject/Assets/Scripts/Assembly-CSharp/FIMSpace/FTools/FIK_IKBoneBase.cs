using System;
using UnityEngine;

namespace FIMSpace.FTools
{
	[Serializable]
	public abstract class FIK_IKBoneBase
	{
		public float sqrMagn;

		public float BoneLength;

		public float MotionWeight;

		public Vector3 InitialLocalPosition;

		public Quaternion InitialLocalRotation;

		public Quaternion LastKeyLocalRotation;

		public FIK_IKBoneBase Child { get; private set; }

		public Transform transform { get; protected set; }

		public FIK_IKBoneBase(Transform t)
		{
		}

		public virtual void SetChild(FIK_IKBoneBase child)
		{
		}
	}
}
