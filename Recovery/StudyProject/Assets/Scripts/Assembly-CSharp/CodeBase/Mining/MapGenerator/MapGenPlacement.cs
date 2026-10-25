using System;

namespace Codebase.Mining.MapGenerator
{
	[Serializable]
	public sealed class MapGenPlacement
	{
		public int Tx;

		public int Ty;

		public float HpMul;

		public float HpAlpha;

		public bool Guard;

		public float GuardMul;

		public int HalfTiles;
	}
}
