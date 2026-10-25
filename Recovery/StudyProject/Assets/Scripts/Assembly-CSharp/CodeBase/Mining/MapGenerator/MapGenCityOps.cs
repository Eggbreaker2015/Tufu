using System;
using System.Collections.Generic;

namespace Codebase.Mining.MapGenerator
{
	internal static class MapGenCityOps
	{
		internal sealed class CityLayout
		{
			public readonly List<MapGenCityBuilding> Buildings;

			public int MinX;

			public int MaxX;

			public int MinY;

			public int MaxY;
		}

		public static int PointDistSqToRect(int x, int y, int minX, int maxX, int minY, int maxY)
		{
			return 0;
		}

		public static CityLayout BuildCityLayout(MapGenPlacement basePlacement, int floor, List<MapGenCityBuildingDef> defs, int gridW, int gridH, MapGenParams pParams)
		{
			return null;
		}

		public static bool CityLayoutFits(CityLayout layout, int gridW, int gridH)
		{
			return false;
		}

		public static MapGenMask[] CarveCityConnections(MapGenCell[] grid, int gridW, int gridH, CityLayout layout, MapGenPlacement basePlacement, int corridorWidthTiles, int spawnerHalfTiles)
		{
			return null;
		}

		public static MapGenPortal BuildCityDownPortal(MapGenCell[] grid, int gridW, int gridH, MapGenMask[] blobMask, MapGenParams p, Func<float> rnd01)
		{
			return null;
		}
	}
}
