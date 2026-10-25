using System;

namespace Codebase.Mining.MapGenerator
{
	internal static class MapGenPortalOps
	{
		private readonly struct PortalDir
		{
			public readonly string Id;

			public readonly int Dx;

			public readonly int Dy;

			public PortalDir(string id, int dx, int dy)
			{
				Id = null;
				Dx = 0;
				Dy = 0;
			}
		}

		private static readonly PortalDir[] Dirs;

		public static bool StampPortal(MapGenCell[] grid, int cx, int cy, string openId, int gridW, int gridH, MapGenParams p)
		{
			return false;
		}

		public static bool CanPlacePortal(MapGenCell[] grid, int cx, int cy, string openId, MapGenMask[] baseSeen, int gridW, int gridH, MapGenParams p, Func<int, int, bool> extraOk = null)
		{
			return false;
		}

		public static MapGenPortals PlacePortals(MapGenCell[] grid, MapGenParams p, int anchorTx, int anchorTy, int gridW, int gridH, Func<int, int, bool> extraOk = null)
		{
			return null;
		}
	}
}
