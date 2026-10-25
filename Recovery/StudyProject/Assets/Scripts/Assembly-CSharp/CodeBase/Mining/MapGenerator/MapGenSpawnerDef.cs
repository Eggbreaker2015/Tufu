using System;

namespace Codebase.Mining.MapGenerator
{
	[Serializable]
	public sealed class MapGenSpawnerDef
	{
		public string Id;

		public bool IsBoss;

		public int MaxAlive;

		public float CounterDamage;

		public string SpawnerType;

		public string SpawnerBehavior;

		public float CooldownT;

		public bool IsUnit;

		public bool IsRunner;

		public bool PatrolWholeMap;

		public bool Guard;

		public float GuardMul;

		public int Tx;

		public int Ty;

		public int HalfTiles;

		public float HpMul;

		public float HpAlpha;

		public MapGenSpawnerDef Clone()
		{
			return null;
		}
	}
}
