using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Rpg.Dungeon
{
	// One enemy group inside a dungeon. Along/Side are metres in the arena's frame:
	// Along runs from the arena's player spawn point towards its boss spawn point, Side is to the right of that.
	[Serializable]
	public class RpgDungeonPack
	{
		public string SpawnerKey;
		public float Along;
		public float Side;
		public int Count = 1;
		public int Level = 1;
	}

	[Serializable]
	public class RpgDungeonDef
	{
		public string Key;
		public string DisplayName;
		public int Island = 1;
		public Vector3 PortalPosition;
		public float PortalRotationY;
		// A Boss Tower arena (BossArenaProvider key); the tower is closed under RPG so its arenas are free.
		public string ArenaKey;
		public float ExitAlong = -4f;
		public List<RpgDungeonPack> Packs = new List<RpgDungeonPack>();
		// Spawned at the arena's boss spawn point; killing it clears the dungeon.
		public RpgDungeonPack Boss = new RpgDungeonPack();
		public float CooldownHours = 24f;
		public float ClearBonusFraction = 0.5f;
	}
}
