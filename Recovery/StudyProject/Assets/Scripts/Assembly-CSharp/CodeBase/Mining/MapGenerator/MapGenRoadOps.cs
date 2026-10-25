using System.Collections.Generic;

namespace Codebase.Mining.MapGenerator
{
	internal static class MapGenRoadOps
	{
		private readonly struct PreferDir
		{
			public readonly int X;

			public readonly int Y;

			public PreferDir(int x, int y)
			{
				X = 0;
				Y = 0;
			}
		}

		private sealed class MinHeap
		{
			private readonly List<(int i, float f)> a;

			public int Size => 0;

			public void Push(int i, float f)
			{
			}

			public int Pop(out float f)
			{
				f = default;
				return 0;
			}
		}

		private static PreferDir DirFromOpen(string open)
		{
			return default;
		}

		public static MapGenMask[] BuildRoads(MapGenCell[] grid, int gridW, int gridH, MapGenParams pParams, short[] distToWall, Dictionary<string, MapGenPlacement> placements, MapGenPlacement basePlacement, MapGenPortals portals, int floor)
		{
			return null;
		}
	}
}
