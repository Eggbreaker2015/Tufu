using System;
using System.Collections.Generic;

namespace Codebase.Mining.MapGenerator
{
	[Serializable]
	public sealed class MapGenResult
	{
		public int Floor;

		public uint Seed;

		public int GridW;

		public int GridH;

		public MapGenCell[] Grid;

		public MapGenWallType[] WallType;

		public MapGenMask[] Roads;

		public int SpawnerHalfTiles;

		public int SpawnerClearHalfTiles;

		public Dictionary<string, MapGenPlacement> Spawners;

		public List<MapGenSpawnerDef> SpawnerDefs;

		public MapGenPortals Portals;

		public List<MapGenCityBuilding> CityBuildings;

		public MapGenBounds CityBounds;

		public List<MapGenTile> DigWalls;

		public List<MapGenTile> Treasures;

		public MapGenPlacement Base;
	}
}
