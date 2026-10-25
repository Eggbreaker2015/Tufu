using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Sphere : FImp_ColliderData_Base
	{
		private float SphereRadius;

		public SphereCollider Sphere { get; private set; }

		public CircleCollider2D Sphere2D { get; private set; }

		public FImp_ColliderData_Sphere(SphereCollider collider)
		{
		}

		public FImp_ColliderData_Sphere(CircleCollider2D collider)
		{
		}

		public override void RefreshColliderData()
		{
		}

		public override bool PushIfInside(ref Vector3 point, float pointRadius, Vector3 pointOffset)
		{
			return false;
		}

		public static bool PushOutFromSphereCollider(SphereCollider sphere, float segmentColliderRadius, ref Vector3 segmentPos, Vector3 segmentOffset)
		{
			return false;
		}

		public static bool PushOutFromSphereCollider(SphereCollider sphere, float segmentColliderRadius, ref Vector3 segmentPos, float collidingSphereRadius, Vector3 segmentOffset)
		{
			return false;
		}

		public static bool PushOutFromSphereCollider(CircleCollider2D sphere, float segmentColliderRadius, ref Vector3 segmentPos, float collidingSphereRadius, Vector3 segmentOffset)
		{
			return false;
		}

		public static float CalculateTrueRadiusOfSphereCollider(SphereCollider sphere)
		{
			return 0f;
		}

		public static float CalculateTrueRadiusOfSphereCollider(CircleCollider2D sphere)
		{
			return 0f;
		}

		public static float CalculateTrueRadiusOfSphereCollider(Transform transform, float componentRadius)
		{
			return 0f;
		}
	}
}
