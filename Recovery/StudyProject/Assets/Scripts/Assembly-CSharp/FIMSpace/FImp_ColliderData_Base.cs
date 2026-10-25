using UnityEngine;

namespace FIMSpace
{
	public abstract class FImp_ColliderData_Base
	{
		public enum EFColliderType
		{
			Box = 0,
			Sphere = 1,
			Capsule = 2,
			Mesh = 3,
			Terrain = 4
		}

		public bool Is2D;

		protected Vector3 previousPosition;

		protected Quaternion previousRotation;

		protected Vector3 previousScale;

		public Transform Transform { get; protected set; }

		public Collider Collider { get; protected set; }

		public Collider2D Collider2D { get; protected set; }

		public bool IsStatic { get; private set; }

		public EFColliderType ColliderType { get; protected set; }

		public static FImp_ColliderData_Base GetColliderDataFor(Collider collider)
		{
			return null;
		}

		public static FImp_ColliderData_Base GetColliderDataFor(Collider2D collider)
		{
			return null;
		}

		public virtual void RefreshColliderData()
		{
		}

		public virtual bool PushIfInside(ref Vector3 point, float pointRadius, Vector3 pointOffset)
		{
			return false;
		}

		public virtual bool PushIfInside2D(ref Vector3 point, float pointRadius, Vector3 pointOffset)
		{
			return false;
		}

		public static bool VIsSame(Vector3 vec1, Vector3 vec2)
		{
			return false;
		}
	}
}
