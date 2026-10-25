using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Box : FImp_ColliderData_Base
	{
		private Vector3 boxCenter;

		private Vector3 right;

		private Vector3 up;

		private Vector3 forward;

		private Vector3 rightN;

		private Vector3 upN;

		private Vector3 forwardN;

		private Vector3 scales;

		public BoxCollider Box { get; private set; }

		public BoxCollider2D Box2D { get; private set; }

		public FImp_ColliderData_Box(BoxCollider collider)
		{
		}

		public FImp_ColliderData_Box(BoxCollider2D collider2D)
		{
		}

		public override void RefreshColliderData()
		{
		}

		public override bool PushIfInside(ref Vector3 segmentPosition, float segmentRadius, Vector3 segmentOffset)
		{
			return false;
		}

		public static void PushOutFromBoxCollider(BoxCollider box, Collision collision, float segmentColliderRadius, ref Vector3 segmentPosition, bool is2D = false)
		{
		}

		public static void PushOutFromBoxCollider(BoxCollider box, float segmentColliderRadius, ref Vector3 segmentPosition, bool is2D = false)
		{
		}

		public static void PushOutFromBoxCollider(BoxCollider box, Collision collision, float segmentColliderRadius, ref Vector3 pos, Vector3 right, Vector3 up, Vector3 forward, Vector3 scales, bool is2D = false)
		{
		}

		public static void PushOutFromBoxCollider(BoxCollider2D box2D, float segmentColliderRadius, ref Vector3 segmentPosition)
		{
		}

		private Vector3 GetNearestPoint(Vector3 point)
		{
			return default;
		}

		private static Vector3 GetNearestPoint(Vector3 point, Vector3 boxCenter, Vector3 right, Vector3 up, Vector3 forward, bool is2D = false)
		{
			return default;
		}

		private static Vector3 GetNearestPoint2D(Vector2 point, Vector2 boxCenter, Vector2 right, Vector2 up)
		{
			return default;
		}

		public static Vector3 GetNearestPointOnBox(BoxCollider boxCollider, Vector3 point, bool is2D = false)
		{
			return default;
		}

		private static float PlaneDistance(Vector3 planeCenter, Vector3 planeNormal, Vector3 point)
		{
			return 0f;
		}

		private static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 point, float distance)
		{
			return default;
		}

		private static bool SphereInsidePlane(float planeDistance, float pointRadius)
		{
			return false;
		}

		private static bool SphereOutsidePlane(float planeDistance, float pointRadius)
		{
			return false;
		}

		private static bool SphereIntersectsPlane(float planeDistance, float pointRadius)
		{
			return false;
		}

		public static bool IsInsideBoxCollider(BoxCollider collider, Vector3 point, bool is2D = false)
		{
			return false;
		}

		public static bool IsInsideBoxCollider(BoxCollider2D collider, Vector3 point)
		{
			return false;
		}

		protected static float GetBoxAverageScale(BoxCollider box)
		{
			return 0f;
		}

		protected static Vector3 GetBoxCenter(BoxCollider box)
		{
			return default;
		}

		protected static Vector3 GetBoxCenter(BoxCollider2D box)
		{
			return default;
		}

		protected static Vector3 GetTargetPlaneNormal(BoxCollider boxCollider, Vector3 point, bool is2D = false)
		{
			return default;
		}

		protected static Vector3 GetTargetPlaneNormal(BoxCollider boxCollider, Vector3 point, Vector3 right, Vector3 up, Vector3 forward, Vector3 scales, bool is2D = false)
		{
			return default;
		}

		protected static Vector3 GetTargetPlaneNormal(BoxCollider2D boxCollider, Vector2 point, Vector2 right, Vector2 up, Vector2 scales)
		{
			return default;
		}

		protected static Vector3 GetIntersectOnBoxFromInside(BoxCollider boxCollider, Vector3 from, Vector3 to, Vector3 planeNormal)
		{
			return default;
		}
	}
}
