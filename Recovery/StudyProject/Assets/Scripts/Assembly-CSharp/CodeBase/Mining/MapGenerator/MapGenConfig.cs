using System;
using System.Collections.Generic;

namespace Codebase.Mining.MapGenerator
{
	[Serializable]
	public sealed class MapGenConfig
	{
		public int Floor;

		public int GridW;

		public int GridH;

		public int SpawnerCount;

		public List<MapGenSpawnerDef> Spawners;

		public List<MapGenCityBuildingDef> CityBuildings;

		public int CorridorRadius;

		public int BaseSeed;

		public MapGenParams Params;

		public float HpStretch;

		public bool UseErodeDilate;

		public float BossHpMul;

		public int DigWallAreasMin;

		public int DigWallAreasMax;

		public int DigWallMinWidth;

		public int DigWallBrushWidth;

		public int DigWallLineLength;

		public string EntryPortal;

		public int MaxAttempts;
	}
}
