using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Capsule : FImp_ColliderData_Base
	{
		private Vector3 Top;

		private Vector3 Bottom;

		private Vector3 Direction;

		private float radius;

		private float scaleFactor;

		private float preRadius;

		public CapsuleCollider Capsule { get; private set; }

		public CapsuleCollider2D Capsule2D { get; private set; }

		public FImp_ColliderData_Capsule(CapsuleCollider collider)
		{
		}

		public FImp_ColliderData_Capsule(CapsuleCollider2D collider)
		{
		}

		public override void RefreshColliderData()
		{
		}

		public override bool PushIfInside(ref Vector3 point, float pointRadius, Vector3 pointOffset)
		{
			return false;
		}

		public static bool PushOutFromCapsuleCollider(CapsuleCollider capsule, float segmentColliderRadius, ref Vector3 pos, Vector3 segmentOffset)
		{
			return false;
		}

		public static bool PushOutFromCapsuleCollider(float segmentColliderRadius, ref Vector3 segmentPos, Vector3 capSphereCenter1, Vector3 capSphereCenter2, float capsuleRadius, Vector3 segmentOffset, bool is2D = false)
		{
			return false;
		}

		protected static void CalculateCapsuleParameters(CapsuleCollider capsule, ref Vector3 direction, ref float trueRadius, ref float scalerFactor)
		{
		}

		private static float GetCapsule2DRadius(CapsuleCollider2D capsule)
		{
			return 0f;
		}

		private static float GetCapsule2DHeight(CapsuleCollider2D capsule)
		{
			return 0f;
		}

		protected static void CalculateCapsuleParameters(CapsuleCollider2D capsule, ref Vector3 direction, ref float trueRadius, ref float scalerFactor)
		{
		}

		protected static void GetCapsuleHeadsPositions(CapsuleCollider capsule, ref Vector3 upper, ref Vector3 bottom, Vector3 direction, float radius, float scalerFactor)
		{
		}

		protected static void GetCapsuleHeadsPositions(CapsuleCollider2D capsule, ref Vector3 upper, ref Vector3 bottom, Vector3 direction, float radius, float scalerFactor)
		{
		}
	}
}
