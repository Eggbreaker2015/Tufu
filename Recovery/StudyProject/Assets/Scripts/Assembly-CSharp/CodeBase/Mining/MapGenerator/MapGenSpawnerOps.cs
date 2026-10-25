using System;
using System.Collections.Generic;

namespace Codebase.Mining.MapGenerator
{
	internal static class MapGenSpawnerOps
	{
		public static bool AreaIsAllFloor(MapGenCell[] grid, int cx, int cy, int half, int gridW, int gridH, Func<int, int, bool> tileOk = null)
		{
			return false;
		}

		public static bool TouchesPlaced(List<(int tx, int ty)> placed, int tx, int ty, int half, MapGenParams pParams)
		{
			return false;
		}

		public static (int, int)? SpiralFind(MapGenCell[] grid, int startX, int startY, int gridW, int gridH, int maxRadius, Func<int, int, bool> accept)
		{
			return null;
		}

		public static (int, int)? FindPlacementNearPortal(MapGenCell[] grid, MapGenPortal portal, int gridW, int gridH, int maxRadiusTiles, MapGenMask[] seenMask, Func<int, int, bool> acceptExtra, int clearHalfOverride, Func<int, int, bool> tileOk = null)
		{
			return null;
		}

		public static bool PlaceOne(MapGenCell[] grid, MapGenParams pParams, int gridW, int gridH, MapGenSpawnerDef def, MapGenMask[] baseSeenForPlacement, List<(int tx, int ty)> placed, Dictionary<string, MapGenPlacement> placements, Func<int, int, bool> tileOk = null)
		{
			return false;
		}

		public static bool RedistributeRegularSpawnerPlacements(MapGenCell[] grid, MapGenParams pParams, int gridW, int gridH, int baseTx, int baseTy, Dictionary<string, MapGenPlacement> placements, List<(int tx, int ty)> placed, List<MapGenSpawnerDef> regularDefs, List<MapGenSpawnerDef> bossDefs, short[] distToWall, Func<int, int, bool> tileOk = null)
		{
			return false;
		}

		public static void ApplySpawnerHpMultipliers(int floor, string entryPortal, float hpStretch, float bossHpMul, Dictionary<string, MapGenPlacement> placements, MapGenPortals portals, MapGenPlacement basePlacement, List<MapGenSpawnerDef> spawners)
		{
		}

		public static void AddGuardSpawnersOnRoads(MapGenMask[] roads, Dictionary<string, MapGenPlacement> placements, MapGenPlacement basePlacement, List<MapGenSpawnerDef> guardDefs, int floor, int gridW, Func<int, int, bool> guardPlacementFilter = null)
		{
		}
	}
}
