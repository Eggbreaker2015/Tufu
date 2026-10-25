using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_CharacterCapsule : FImp_ColliderData_Base
	{
		private Vector3 Top;

		private Vector3 Bottom;

		private Vector3 Direction;

		private float radius;

		private float scaleFactor;

		private float preRadius;

		public CharacterController Capsule { get; private set; }

		public FImp_ColliderData_CharacterCapsule(CharacterController collider)
		{
		}

		public override void RefreshColliderData()
		{
		}

		public override bool PushIfInside(ref Vector3 point, float pointRadius, Vector3 pointOffset)
		{
			return false;
		}

		public static bool PushOutFromCapsuleCollider(float segmentColliderRadius, ref Vector3 segmentPos, Vector3 capSphereCenter1, Vector3 capSphereCenter2, float capsuleRadius, Vector3 segmentOffset, bool is2D = false)
		{
			return false;
		}

		protected static void CalculateCapsuleParameters(CharacterController capsule, ref Vector3 direction, ref float trueRadius, ref float scalerFactor)
		{
		}

		protected static void GetCapsuleHeadsPositions(CharacterController capsule, ref Vector3 upper, ref Vector3 bottom, Vector3 direction, float radius, float scalerFactor)
		{
		}
	}
}
