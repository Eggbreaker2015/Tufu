using System.Collections.Generic;
using Codebase.Core;

namespace Codebase.Mining.MapGenerator
{
	public static class MapGenCaveGenerator
	{
		public static string LastFailReason { get; private set; }

		private static MapGenResult SetFail(string reason)
		{
			return null;
		}

		public static MapGenResult GenerateCave(MapGenConfig cfg)
		{
			return null;
		}

		public static MapGenResult GenerateFallbackCave(MapGenConfig cfg)
		{
			return null;
		}

		private static MapGenResult GenerateFallbackCaveNoInit(MapGenConfig cfg, int initState)
		{
			return null;
		}

		public static MapGenResult GenerateCaveOnce(MapGenConfig cfg)
		{
			return null;
		}

		private static MapGenResult GenerateCaveOnceNoInit(MapGenConfig cfg, int initState)
		{
			return null;
		}

		private static List<MapGenSpawnerDef> BuildDefaultSpawners(int spawnerCount)
		{
			return null;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
		}
	}
}
