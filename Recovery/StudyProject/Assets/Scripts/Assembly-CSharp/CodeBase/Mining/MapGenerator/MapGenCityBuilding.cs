using System;

namespace Codebase.Mining.MapGenerator
{
	[Serializable]
	public sealed class MapGenCityBuilding
	{
		public string Id;

		public string Name;

		public int SizeTiles;

		public int UnlockFloor;

		public bool Interactive;

		public int Tx;

		public int Ty;
	}
}
