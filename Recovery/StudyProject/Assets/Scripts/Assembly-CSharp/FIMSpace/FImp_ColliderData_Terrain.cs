using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Terrain : FImp_ColliderData_Base
	{
		public TerrainCollider TerrCollider { get; private set; }

		public Terrain TerrainComponent { get; private set; }

		public FImp_ColliderData_Terrain(TerrainCollider collider)
		{
		}

		public override bool PushIfInside(ref Vector3 segmentPosition, float segmentRadius, Vector3 segmentOffset)
		{
			return false;
		}

		public static void PushOutFromTerrain(TerrainCollider terrainCollider, float segmentRadius, ref Vector3 point)
		{
		}
	}
}
