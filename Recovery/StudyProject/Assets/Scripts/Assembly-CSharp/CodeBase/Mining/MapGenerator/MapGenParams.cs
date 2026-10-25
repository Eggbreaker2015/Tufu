using System;

namespace Codebase.Mining.MapGenerator
{
	[Serializable]
	public sealed class MapGenParams
	{
		public int SeedMinDist;

		public int SeedMargin;

		public int ExtraSeeds;

		public float BigSeedChance;

		public int BlobBaseSteps;

		public int BlobBaseBrushMin;

		public int BlobBaseBrushMax;

		public int BlobSpawnerSteps;

		public int BlobSpawnerBrushMin;

		public int BlobSpawnerBrushMax;

		public int BlobBigSteps;

		public int BlobBigBrushMin;

		public int BlobBigBrushMax;

		public int BlobSmallSteps;

		public int BlobSmallBrushMin;

		public int BlobSmallBrushMax;

		public int SmoothIterations;

		public int MinRoomSize;

		public int MaxRooms;

		public int SpawnerHalfTiles;

		public int SpawnerClearHalfTiles;

		public int SpawnerMinEdgeGapTiles;

		public int SpawnerMaxTries;

		public int SpawnerSpiralMaxRadius;

		public int SpawnerSpiralRestarts;

		public int BaseNearDownTiles;

		public int RoadMinWallDist;

		public int RoadAnchorMinR;

		public int RoadAnchorMaxR;

		public int RoadPortalOffset;

		public int RoadPortalAnchorMaxR;

		public float RoadNoise;

		public int PortalSizeTiles;

		public int PortalOuterSizeTiles;

		public int PortalMinDistTiles;

		public int PortalMaxTries;

		public int PortalOpenSideFloorMin;

		public int PortalBias;

		public int BossNearPortalTiles;

		public int CityBuildingGapTiles;

		public int CityCorridorWidthTiles;

		public int CitySeedKeepOutTiles;

		public int CityPortalKeepOutTiles;
	}
}
