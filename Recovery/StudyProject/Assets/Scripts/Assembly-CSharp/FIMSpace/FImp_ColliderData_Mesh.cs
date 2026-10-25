using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Mesh : FImp_ColliderData_Base
	{
		private ContactFilter2D filter;

		private RaycastHit2D[] r;

		public MeshCollider Mesh { get; private set; }

		public PolygonCollider2D Poly2D { get; private set; }

		public FImp_ColliderData_Mesh(MeshCollider collider)
		{
		}

		public FImp_ColliderData_Mesh(PolygonCollider2D collider)
		{
		}

		public override bool PushIfInside(ref Vector3 segmentPosition, float segmentRadius, Vector3 segmentOffset)
		{
			return false;
		}

		public static void PushOutFromMeshCollider(MeshCollider mesh, Collision collision, float segmentColliderRadius, ref Vector3 pos)
		{
		}

		public static void PushOutFromMesh(MeshCollider mesh, Collision collision, float pointRadius, ref Vector3 point)
		{
		}
	}
}
