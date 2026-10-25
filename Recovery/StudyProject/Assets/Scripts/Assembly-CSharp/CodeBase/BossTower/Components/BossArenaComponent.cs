using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct BossArenaComponent : IComponent
	{
		public string ArenaKey;
		public Transform BossSpawnPoint;
		public Transform PlayerSpawnPoint;
		public Transform[] SpiritSpawnPoints;
		public Bounds ArenaBounds;
		public Vector3 GetRandomPointInArena()
		{
			return new Vector3(UnityEngine.Random.Range(ArenaBounds.min.x, ArenaBounds.max.x), ArenaBounds.center.y, UnityEngine.Random.Range(ArenaBounds.min.z, ArenaBounds.max.z));
		}

		public Vector3 GetHalfArenaCenter(bool leftHalf)
		{
			return new Vector3(ArenaBounds.center.x + (leftHalf ? -ArenaBounds.extents.x * 0.5f : ArenaBounds.extents.x * 0.5f), ArenaBounds.center.y, ArenaBounds.center.z);
		}

		public Vector3[] GetArenaCorners()
		{
			Vector3 c = ArenaBounds.center, e = ArenaBounds.extents;
			return new[]
			{
				new Vector3(c.x - e.x, c.y, c.z - e.z),
				new Vector3(c.x + e.x, c.y, c.z - e.z),
				new Vector3(c.x - e.x, c.y, c.z + e.z),
				new Vector3(c.x + e.x, c.y, c.z + e.z)
			};
		}

		public Vector3 GetRandomCorner()
		{
			Vector3[] corners = GetArenaCorners();
			return corners[UnityEngine.Random.Range(0, corners.Length)];
		}
	}
}
