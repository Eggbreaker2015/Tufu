using System.Collections.Generic;

namespace Codebase.Mining.MapGenerator
{
	internal static class MapGenGridOps
	{
		internal readonly struct RoomComponent
		{
			public readonly int Tx;

			public readonly int Ty;

			public readonly int Count;

			public RoomComponent(int tx, int ty, int count)
			{
				Tx = 0;
				Ty = 0;
				Count = 0;
			}
		}

		public static int IdxOf(int x, int y, int gridW)
		{
			return 0;
		}

		public static bool InBounds(int x, int y, int gridW, int gridH)
		{
			return false;
		}

		public static bool IsWallAt(MapGenCell[] grid, int x, int y, int gridW, int gridH)
		{
			return false;
		}

		public static bool IsFloorAt(MapGenCell[] grid, int x, int y, int gridW, int gridH)
		{
			return false;
		}

		public static int CountWall8(MapGenCell[] grid, int x, int y, int gridW, int gridH)
		{
			return 0;
		}

		public static void CarveDisk(MapGenCell[] grid, int cx, int cy, int rTiles, int gridW, int gridH)
		{
		}

		public static void CarveBlob(MapGenCell[] grid, int cx, int cy, int steps, int brushMin, int brushMax, int gridW, int gridH)
		{
		}

		public static void SmoothCave(MapGenCell[] grid, int iterations, int gridW, int gridH)
		{
		}

		public static MapGenMask[] FloodFrom(MapGenCell[] grid, int sx, int sy, int gridW, int gridH)
		{
			return null;
		}

		public static List<RoomComponent> CollectFloorComponents(MapGenCell[] grid, int gridW, int gridH)
		{
			return null;
		}
	}
}
