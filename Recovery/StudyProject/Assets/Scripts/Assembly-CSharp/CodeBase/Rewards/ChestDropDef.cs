using System;
using Codebase.Player.Inventory;

namespace Codebase.Rewards
{
	[Serializable]
	public class ChestDropDef
	{
		public ItemType ItemType;

		public Rarity Rarity;

		public float Weight;
	}
}
